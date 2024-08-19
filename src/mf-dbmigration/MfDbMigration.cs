using System.Data.SqlClient;
using System.Text;
using System.Text.RegularExpressions;
using EvolveDb;
using EvolveDb.Configuration;
using EvolveDb.Dialect;
using Microsoft.Extensions.Configuration;
// ReSharper disable ConvertToUsingDeclaration

namespace Mf.DbMigration;

// ReSharper disable once InconsistentNaming
internal class MfDbMigration(string Command)
{
    public void Execute(
        CommonParameters commonParams)
    {
        try
        {
            if (commonParams.File is not null
                && !Path.Exists(commonParams.File))
            {
                throw new ArgumentException("It was not possible to find the expected definitions file.");
            }

            if (commonParams.File is not null
                && Path.Exists(commonParams.File))
            {
                IConfigurationRoot config = new ConfigurationBuilder()
                    .AddJsonFile("appsettings.json", optional: false)
                    .Build();

                MaskingConfig maskingConfig = config.GetSection("Masking")
                    .Get<MaskingConfig>();

                Console.WriteLine();
                Console.WriteLine($"""
                                   {GlobalProperties.ConsoleOutputRuler}
                                   Loaded '{commonParams.File}' file:

                                   """);

                using (StreamReader streamReader = new(commonParams.File))
                {
                    Regex connectionStringEntryMatch = new(maskingConfig.YamlEntryConnectionStringRegex.Match);

                    while (streamReader.ReadLine()! is { } line)
                    {
                        Match match = connectionStringEntryMatch.Match(line);

                        if (match is
                            {
                                Success: true,
                                Groups.Count: 2
                            })
                        {
                            string keySegment = match.Groups[0].Value;
                            string valueSegment = match.Groups[1].Value;

                            valueSegment = maskingConfig.ConnectionStringValueRegex
                                .Aggregate(
                                    valueSegment,
                                    (current, iRegexConfig) 
                                        => Regex.Replace(
                                            current,
                                            iRegexConfig.Match,
                                            iRegexConfig.Replace));

                            line = keySegment + valueSegment;
                        }

                        Console.WriteLine(line);
                    }
                }

                Console.WriteLine(GlobalProperties.ConsoleOutputRuler);
            }

            MfMigrationDefinitions definitions = commonParams.ToJbMigration();

            Console.WriteLine();
            Console.WriteLine($"""
                               {GlobalProperties.ConsoleOutputRuler}
                               Loaded definitions:

                               """);
            Console.Write(definitions.ToString());
            Console.WriteLine(GlobalProperties.ConsoleOutputRuler);

            using (SqlConnection sqlConnection = new(definitions.ConnectionString!))
            {
                Evolve evolve = new(
                        sqlConnection,
                        Console.WriteLine,
                        definitions.Dbms switch
                        {
                            "mysql" => DBMS.MySQL,
                            "mariadb" => DBMS.MariaDB,
                            "oracle" => DBMS.Oracle,
                            "postgresql" => DBMS.PostgreSQL,
                            "sqlite" => DBMS.SQLite,
                            "sqlserver" => DBMS.SQLServer,
                            "cassandra" => DBMS.Cassandra,
                            "cockroachdb" => DBMS.CockroachDB,
                            _ => DBMS.SQLite
                        })
                    {
                        Command = Command switch
                        {
                            "migrate" => CommandOptions.Migrate,
                            "repair" => CommandOptions.Repair,
                            "erase" => CommandOptions.Erase,
                            "info" => CommandOptions.Info,
                            "validate" => CommandOptions.Validate,
                            _ => CommandOptions.DoNothing
                        },
                        Locations = definitions.Location,
                        Schemas = definitions.Schemas!,
                        PlaceholderPrefix = definitions.PlaceholderPrefix,
                        PlaceholderSuffix = definitions.PlaceholderSuffix,
                        Placeholders = definitions.Placeholder ?? new(),
                        MetadataTableName = definitions.MetadataTable,
                        MetadataTableSchema = definitions.MetadataSchema!,
                        EnableClusterMode = definitions.EnableClusterMode,
                        SqlMigrationPrefix = definitions.SqlMigrationPrefix,
                        SqlRepeatableMigrationPrefix = definitions.SqlRepeatableMigrationPrefix,
                        SqlMigrationSeparator = definitions.SqlMigrationSeparator,
                        SqlMigrationSuffix = definitions.SqlMigrationSuffix,
                        CommandTimeout = definitions.CommandTimeout,
                        Encoding = Encoding.GetEncoding(definitions.Encoding),
                        RetryRepeatableMigrationsUntilNoError = definitions.RetryRepeatableMigrationsUntilNoError,
                        SkipNextMigrations = definitions.SkipNextMigrations,
                        OutOfOrder = definitions.OutOfOrder,
                        MustEraseOnValidationError = definitions.EraseOnValidationError,
                        IsEraseDisabled = definitions.EraseDisabled,
                        TransactionMode = definitions.TransactionMode switch
                        {
                            MfMigrationDefinitions.TransactionModeCommitEach => TransactionKind.CommitEach,
                            MfMigrationDefinitions.TransactionModeCommitAll => TransactionKind.CommitAll,
                            MfMigrationDefinitions.TransactionModeRollbackAll => TransactionKind.RollbackAll,
                            _ => TransactionKind.RollbackAll
                        }
                    };
                
                evolve.ExecuteCommand();
            }
        }
        catch (Exception ex)
        {
            ex.Exit();
        }

        Console.WriteLine(GlobalProperties.ConsoleOutputRuler);
        Console.WriteLine(GlobalProperties.ExitingQuote);
        
        Environment.Exit(0);
    }
}

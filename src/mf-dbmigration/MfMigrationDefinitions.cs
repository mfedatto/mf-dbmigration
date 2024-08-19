using System.Text;
using System.Text.RegularExpressions;
using Microsoft.Extensions.Configuration;
// ReSharper disable PropertyCanBeMadeInitOnly.Global

namespace Mf.DbMigration;

public class MfMigrationDefinitions
{
    public const string TransactionModeCommitEach = "CommitEach";
    public const string TransactionModeCommitAll = "CommitAll";
    public const string TransactionModeRollbackAll = "RollbackAll";
    
    public string? Dbms { get; set; }
    public string? ConnectionString { get; set; }
    public string[] Location { get; set; } = new[] { "Sql_Scripts" };
    public string[]? Schemas { get; set; }
    public string PlaceholderPrefix { get; set; } = "${";
    public string PlaceholderSuffix { get; set; } = "{";
    public Dictionary<string, string>? Placeholder { get; set; }
    public string MetadataTable { get; set; } = "changelog";
    public string? MetadataSchema { get; set; } = null;
    public bool EnableClusterMode { get; set; } = true;
    public string SqlMigrationPrefix { get; set; } = "V";
    public string SqlRepeatableMigrationPrefix { get; set; } = "R";
    public string SqlMigrationSeparator { get; set; } = "__";
    public string SqlMigrationSuffix { get; set; } = ".sql";
    public int? CommandTimeout { get; set; } = null;
    public string Encoding { get; set; } = "UTF-8";
    public bool RetryRepeatableMigrationsUntilNoError { get; set; } = false;
    public bool SkipNextMigrations { get; set; } = false;
    public bool OutOfOrder { get; set; } = false;
    public bool EraseOnValidationError { get; set; } = false;
    public bool EraseDisabled { get; set; } = true;
    public string TransactionMode { get; set; } = TransactionModeCommitEach;

    public string ToString(bool demo = false)
    {
        IConfigurationRoot config = new ConfigurationBuilder()
            .AddJsonFile("appsettings.json", optional: false)
            .Build();

        MaskingConfig maskingConfig = config.GetSection("Masking")
            .Get<MaskingConfig>();

        StringBuilder resultBuilder = new();

        resultBuilder.AppendLine($"dbms: {Dbms ?? "sqlserver"}");

        if (ConnectionString is not null)
        {
            string connectionString = ConnectionString;

            foreach (MaskingConfig.RegexConfig regexConfig in maskingConfig.ConnectionStringValueRegex)
            {
                connectionString = Regex.Replace(
                    ConnectionString,
                    regexConfig.Match,
                    regexConfig.Replace);
            }

            resultBuilder.AppendLine($"connection-string: {connectionString}");
        }
        else if (demo)
        {
            resultBuilder.AppendLine("connection-string: _example_connection_string");
        }

        resultBuilder.AppendLine("location:");

        if (Location.Length != 0)
        {
            foreach (string iLocation in Location)
            {
                resultBuilder.AppendLine($"- {iLocation}");
            }
        }
        else if (demo)
        {
            resultBuilder.AppendLine("- _example_/db/migrations/versioned");
            resultBuilder.AppendLine("- _example_/db/migrations/repeatable");
            resultBuilder.AppendLine("- _example_/db/migrations/dataset");
        }

        resultBuilder.AppendLine("schemas:");

        if (Schemas is not null
            && Schemas!.Length != 0)
        {
            foreach (string iSchema in Schemas!)
            {
                resultBuilder.AppendLine($"- {iSchema}");
            }
        }
        else if (demo)
        {
            resultBuilder.AppendLine("- _example_public");
            resultBuilder.AppendLine("- _example_unittests");
        }

        resultBuilder.AppendLine($"placeholder-prefix: {PlaceholderPrefix}");
        resultBuilder.AppendLine($"placeholder-suffix: {PlaceholderSuffix}");
        resultBuilder.AppendLine("placeholder:");

        if (Placeholder is not null
            && Placeholder!.Count != 0)
        {
            foreach (KeyValuePair<string, string> kvPlaceholder in Placeholder!)
            {
                resultBuilder.AppendLine($"  {kvPlaceholder.Key}: {kvPlaceholder.Value}");
            }
        }
        else if (demo)
        {
            resultBuilder.AppendLine("  _example_fruit: orange");
            resultBuilder.AppendLine("  _example_planet: earth");
            resultBuilder.AppendLine("  _example_engine: v10");
        }

        resultBuilder.AppendLine($"metadata-table: {MetadataTable}");
        resultBuilder.AppendLine($"metadata-schema: {MetadataSchema}");
        resultBuilder.AppendLine($"enable-cluster-mode: {EnableClusterMode.ToString().ToLower()}");
        resultBuilder.AppendLine($"sql-migration-prefix: {SqlMigrationPrefix}");
        resultBuilder.AppendLine($"sql-repeatable-migration-prefix: {SqlRepeatableMigrationPrefix}");
        resultBuilder.AppendLine($"sql-migration-separator: {SqlMigrationSeparator}");
        resultBuilder.AppendLine($"sql-migration-suffix: {SqlMigrationSuffix}");
        resultBuilder.AppendLine($"command-timeout: {CommandTimeout}");
        resultBuilder.AppendLine($"encoding: {Encoding}");
        resultBuilder.AppendLine($"retry-repeatable-migrations-until-no-error: {RetryRepeatableMigrationsUntilNoError.ToString().ToLower()}");
        resultBuilder.AppendLine($"skip-next-migrations: {SkipNextMigrations.ToString().ToLower()}");
        resultBuilder.AppendLine($"out-of-order: {OutOfOrder.ToString().ToLower()}");
        resultBuilder.AppendLine($"erase-on-validation-error: {EraseOnValidationError.ToString().ToLower()}");
        resultBuilder.AppendLine($"erase-disabled: {EraseDisabled.ToString().ToLower()}");
        resultBuilder.AppendLine($"transaction-mode: {TransactionMode}");

        return resultBuilder.ToString();
    }
}

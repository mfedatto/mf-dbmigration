using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

namespace Mf.DbMigration;

internal static class CommonParametersExtensions
{
    public static MfMigrationDefinitions ToJbMigration(
        this CommonParameters commonParams)
    {
        if (commonParams.File is not null
            && !Path.Exists(commonParams.File))
        {
            throw new ArgumentException("It was not possible to find the expected definitions file.");
        }
        
        if (commonParams.File is not null
            && Path.Exists(commonParams.File))
        {
            return new DeserializerBuilder()
                .WithNamingConvention(HyphenatedNamingConvention.Instance)
                .Build()
                .Deserialize<MfMigrationDefinitions>(
                    File.ReadAllText(commonParams.File));
        }

        if (commonParams.File is not null)
            throw new ArgumentException("It was not possible to determine the definitions for the specified command.");
        if (commonParams.Database is null)
            throw new ArgumentException("It was not possible to determine the target DBMS.");
        if (commonParams.ConnectionString is null)
            throw new ArgumentException("It was not possible to determine the connection string.");
        if (commonParams.Location is null)
            throw new ArgumentException("It was not possible to determine the scripts location.");
            
        return new()
        {
            Dbms = commonParams.Database!,
            ConnectionString = commonParams.ConnectionString!,
            Location = commonParams.Location!,
            Schemas = commonParams.Schema,
            Placeholder = commonParams.Placeholder?
                .Select(
                    placeholder =>
                    {
                        int indexOfSeparator = placeholder.IndexOf(':');
                        string placeholderKey = placeholder[..indexOfSeparator];
                        string placeholderValue = placeholder[indexOfSeparator..];

                        return new KeyValuePair<string, string>(
                            placeholderKey,
                            placeholderValue);
                    })
                .ToDictionary(),
            MetadataTable = commonParams.MetadataTable!
        };
    }
}

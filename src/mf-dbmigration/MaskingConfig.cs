namespace Mf.DbMigration;

public class MaskingConfig
{
    public required RegexConfig YamlEntryConnectionStringRegex { get; set; }
    public required RegexConfig[] ConnectionStringValueRegex { get; set; }

    public class RegexConfig
    {
        public required string Match { get; set;}
        public required string Replace { get; set; }
    }
}

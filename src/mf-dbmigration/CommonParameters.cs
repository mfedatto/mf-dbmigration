using Cocona;
// ReSharper disable NotAccessedPositionalProperty.Global

namespace Mf.DbMigration;

// ReSharper disable once ClassNeverInstantiated.Global
internal record CommonParameters(
    [Argument]
    string? Database,
    [Option('c')]
    string? ConnectionString,
    [Option('l')]
    string[]? Location,
    [Option('s')]
    string[]? Schema,
    [Option('p')]
    string[]? Placeholder,
    [Option]
    string? MetadataTable,
    [Option('t')]
    string? TransactionMode,
    [Option('f')]
    string? File = "migration.yml"
) : ICommandParameterSet;

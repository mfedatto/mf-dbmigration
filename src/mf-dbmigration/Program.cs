using System.Runtime.InteropServices;
using System.Security.Principal;
using Cocona;
using Cocona.Builder;
using Mf.DbMigration;
using Mono.Unix;

bool? hasElevatedPrivileges = HasElevatedPrivileges();
string elevatedUserIndication = hasElevatedPrivileges is null
    ? " (unknown if elevated or not)"
    : (bool)hasElevatedPrivileges
        ? " (elevated)"
        : "";

Console.WriteLine($"""
                   {GlobalProperties.ConsoleOutputRuler}
                   Mauricio Fedatto
                   DB Migration
                   {DateTime.Now:yyyy-MM-dd HH:mm:ss zz}
                   {GlobalProperties.ConsoleOutputRuler}
                   Runtime identifier:
                     {RuntimeInformation.RuntimeIdentifier}
                   Framework description:
                     {RuntimeInformation.FrameworkDescription}
                   Process architecture:
                     {RuntimeInformation.ProcessArchitecture}
                   OS architecture:
                     {RuntimeInformation.OSArchitecture}
                   OS description:
                     {RuntimeInformation.OSDescription}
                   Working directory:
                     {Directory.GetCurrentDirectory()}
                   Command line:
                     {Environment.CommandLine}
                   Hostname:
                     {Environment.MachineName}
                   User:
                     {Environment.UserName}{elevatedUserIndication}
                   {GlobalProperties.ConsoleOutputRuler}
                   """);

try
{
    CoconaAppBuilder coconaAppBuilder = CoconaApp.CreateBuilder();
    CoconaApp coconaApp = coconaAppBuilder.Build();
    
    coconaApp.AddCommand(
        name: "migrate",
        commandBody: new MfDbMigration("migrate").Execute);
    coconaApp.AddCommand(
        name: "repair",
        commandBody: new MfDbMigration("repair").Execute);
    coconaApp.AddCommand(
        name: "erase",
        commandBody: new MfDbMigration("erase").Execute);
    coconaApp.AddCommand(
        name: "info",
        commandBody: new MfDbMigration("info").Execute);
    coconaApp.AddCommand(
        name: "validate",
        commandBody: new MfDbMigration("validate").Execute);
    coconaApp.AddCommand(
        name: "example",
        commandBody: () =>
        {
            Console.WriteLine(
                new MfMigrationDefinitions()
                    .ToString(demo: true));

            Environment.Exit(0);
        });
    
    coconaApp.Run();
    
    Console.WriteLine(GlobalProperties.ExitingQuote);
}
catch (Exception ex)
{
    ex.Exit();
}

return 0;

bool? HasElevatedPrivileges()
{
    if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
    {
        WindowsIdentity identity = WindowsIdentity.GetCurrent();
        WindowsPrincipal principal = new(identity);
            
        return principal.IsInRole(WindowsBuiltInRole.Administrator);
    }
    
    if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
    {
        UnixUserInfo userInfo = new(UnixEnvironment.UserName);
            
        return userInfo.UserId == 0;
    }

    return null;
}

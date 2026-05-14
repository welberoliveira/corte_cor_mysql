using Microsoft.Extensions.Configuration;

namespace CorteCor.Handlers;

internal static class AppConfigurationFactory
{
    public static IConfigurationRoot Build()
    {
        var environmentName =
            Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT")
            ?? Environment.GetEnvironmentVariable("DOTNET_ENVIRONMENT")
            ?? "Development";

        var baseDirectory = ResolveBaseDirectory();

        return new ConfigurationBuilder()
            .SetBasePath(baseDirectory)
            .AddJsonFile("appsettings.json", optional: true, reloadOnChange: true)
            .AddJsonFile($"appsettings.{environmentName}.json", optional: true, reloadOnChange: true)
            .AddJsonFile("appsettings.Local.json", optional: true, reloadOnChange: true)
            .AddEnvironmentVariables()
            .Build();
    }

    private static string ResolveBaseDirectory()
    {
        foreach (var origin in new[]
                 {
                     Directory.GetCurrentDirectory(),
                     AppContext.BaseDirectory
                 })
        {
            if (string.IsNullOrWhiteSpace(origin))
            {
                continue;
            }

            var current = new DirectoryInfo(origin);
            while (current != null)
            {
                if (File.Exists(Path.Combine(current.FullName, "appsettings.Local.json")) ||
                    File.Exists(Path.Combine(current.FullName, "CorteCor.csproj")))
                {
                    return current.FullName;
                }

                current = current.Parent;
            }
        }

        return Directory.GetCurrentDirectory();
    }
}

using Kyvo.Application.Services.Tenancy;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.Configuration;

namespace Kyvo.Infrastructure.Persistence;

public sealed class ApplicationDbContextFactory : IDesignTimeDbContextFactory<ApplicationDbContext>
{
    public ApplicationDbContext CreateDbContext(string[] args)
    {
        var connectionString = ResolveConnectionString();
        var optionsBuilder = new DbContextOptionsBuilder<ApplicationDbContext>();
        optionsBuilder.UseNpgsql(connectionString);
        return new ApplicationDbContext(optionsBuilder.Options, new TenantContext());
    }

    private static string ResolveConnectionString()
    {
        var apiProjectPath = ResolveApiProjectPath();
        var configuration = new ConfigurationBuilder()
            .SetBasePath(apiProjectPath)
            .AddJsonFile("appsettings.json", optional: false)
            .AddJsonFile("appsettings.Development.json", optional: true)
            .AddEnvironmentVariables()
            .Build();

        return configuration.GetConnectionString("Default")
            ?? configuration["Database:ConnectionString"]
            ?? "Host=localhost;Port=5433;Database=kyvo_idp;Username=kyvo;Password=kyvo";
    }

    private static string ResolveApiProjectPath()
    {
        var directory = new DirectoryInfo(Directory.GetCurrentDirectory());
        while (directory is not null)
        {
            foreach (var name in new[] { "Kyvo.API", "Kyvo.API" })
            {
                var apiPath = Path.Combine(directory.FullName, name);
                if (Directory.Exists(apiPath))
                {
                    return apiPath;
                }
            }

            directory = directory.Parent;
        }

        throw new InvalidOperationException("Could not locate the API project directory for EF design-time.");
    }
}

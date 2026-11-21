using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.Configuration;

namespace SuperShop.Data
{
    public class ApplicationDbContextFactory
        : IDesignTimeDbContextFactory<ApplicationDbContext>
    {
        public ApplicationDbContext CreateDbContext(string[] args)
        {
            // The SuperShop project directory
            var projectDirectory = Path.Combine(
                Directory.GetCurrentDirectory(),
                "SuperShop"
            );

            // If Package Manager Console is already inside
            // the SuperShop project directory, use that instead.
            if (File.Exists(
                Path.Combine(
                    Directory.GetCurrentDirectory(),
                    "appsettings.json"
                )))
            {
                projectDirectory = Directory.GetCurrentDirectory();
            }

            var configuration = new ConfigurationBuilder()
                .SetBasePath(projectDirectory)
                .AddJsonFile(
                    "appsettings.json",
                    optional: false,
                    reloadOnChange: false
                )
                .AddEnvironmentVariables()
                .Build();

            var connectionString =
                configuration.GetConnectionString("DefaultConnection");

            if (string.IsNullOrWhiteSpace(connectionString))
            {
                throw new InvalidOperationException(
                    "Database connection string 'DefaultConnection' was not found in appsettings.json."
                );
            }

            var optionsBuilder =
                new DbContextOptionsBuilder<ApplicationDbContext>();

            optionsBuilder.UseMySql(
                connectionString,
                ServerVersion.AutoDetect(connectionString)
            );

            return new ApplicationDbContext(
                optionsBuilder.Options
            );
        }
    }
}
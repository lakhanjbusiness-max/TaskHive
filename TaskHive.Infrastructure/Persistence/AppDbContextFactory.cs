using System;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using TaskHive.Application.Common;

namespace TaskHive.Infrastructure.Persistence
{
    // Used ONLY by the dotnet-ef tooling at design time. Never runs in the application.
    public class AppDbContextFactory : IDesignTimeDbContextFactory<AppDbContext>
    {
        public AppDbContext CreateDbContext(string[] args)
        {
            // Env var lets CI point migrations at Azure SQL later without a code change.
            var connectionString =
                Environment.GetEnvironmentVariable("TASKHIVE_MIGRATIONS_CONNECTION")
                ?? "Server=(localdb)\\MSSQLLocalDB;Database=TaskHive;" +
                   "Trusted_Connection=True;TrustServerCertificate=True";

            var options = new DbContextOptionsBuilder<AppDbContext>()
                .UseSqlServer(connectionString)
                .Options;

            return new AppDbContext(options, new DesignTimeTenantContext());
        }

        // Migrations generate SCHEMA. Query filters only affect QUERIES. So a null
        // tenant here is not a shortcut - it genuinely cannot influence the output.
        private sealed class DesignTimeTenantContext : ITenantContext
        {
            public Guid? TenantId => null;
        }
    }
}
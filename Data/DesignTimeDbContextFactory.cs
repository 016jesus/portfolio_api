using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace portfolio_api.Data;

/// <summary>
/// Factory used by EF Core CLI tools (dotnet ef migrations) at design time.
/// The connection string is only used to generate SQL; no real connection is
/// opened during migration scaffolding.
/// </summary>
public class DesignTimeDbContextFactory : IDesignTimeDbContextFactory<ApplicationDbContext>
{
    public ApplicationDbContext CreateDbContext(string[] args)
    {
        var connectionString = Environment.GetEnvironmentVariable("PORTGRES_CONNECTION")
            ?? "Host=localhost;Database=portfolio_design;Username=postgres;Password=postgres";

        var optionsBuilder = new DbContextOptionsBuilder<ApplicationDbContext>();
        optionsBuilder.UseNpgsql(connectionString);

        // Provide a no-op tenant provider for design-time usage
        return new ApplicationDbContext(optionsBuilder.Options, new DesignTimeTenantProvider());
    }

    private class DesignTimeTenantProvider : ITenantProvider
    {
        public Guid? TenantId => null;
    }
}

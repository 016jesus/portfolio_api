using Microsoft.EntityFrameworkCore;
using portfolio_api.Models;


namespace portfolio_api.Data;

public class ApplicationDbContext : DbContext
{
    private readonly ITenantProvider _tenantProvider;

    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options, ITenantProvider tenantProvider)
        : base(options)
    {
        _tenantProvider = tenantProvider;
    }

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        base.OnConfiguring(optionsBuilder);

        // Aumentar timeout para comandos
        if (optionsBuilder.IsConfigured)
        {
            optionsBuilder.UseNpgsql(o => o.CommandTimeout(120));
        }
    }

    // Agrega tus DbSets aquí, por ejemplo:
    public DbSet<Project> Projects { get; set; }
    public DbSet<User> Users { get; set; }
    public DbSet<Skill> Skills { get; set; }
    public DbSet<Technology> Technologies { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<User>()
            .HasKey(u => new { u.Id } );

        modelBuilder.Entity<Project>()
            .HasOne(p => p.User)
            .WithMany(p => p.Projects)
            .HasForeignKey(p => new { p.UserId})
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<Skill>()
            .HasOne(s => s.User)
            .WithMany(u => u.Skills)
            .HasForeignKey(s => new { s.UserId })
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<User>()
            .HasQueryFilter(u => _tenantProvider.TenantId != null && u.TenantId == _tenantProvider.TenantId.GetValueOrDefault());

        modelBuilder.Entity<Project>()
            .HasQueryFilter(p => _tenantProvider.TenantId != null && p.TenantId == _tenantProvider.TenantId.GetValueOrDefault());

        modelBuilder.Entity<Skill>()
            .HasQueryFilter(s => _tenantProvider.TenantId != null && s.TenantId == _tenantProvider.TenantId.GetValueOrDefault());

        modelBuilder.Entity<Technology>()
            .HasQueryFilter(t => _tenantProvider.TenantId != null && t.TenantId == _tenantProvider.TenantId.GetValueOrDefault());

        // Default values for portfolio fields
        modelBuilder.Entity<Project>()
            .Property(p => p.IsVisible)
            .HasDefaultValue(true);

        modelBuilder.Entity<User>()
            .Property(u => u.ShowGitHubReposAsDefault)
            .HasDefaultValue(true);

        modelBuilder.Entity<User>()
            .Property(u => u.HiddenRepoIds)
            .HasDefaultValue("[]");
    }
}

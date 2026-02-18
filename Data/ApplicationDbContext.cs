using Microsoft.EntityFrameworkCore;
using portfolio_api.Models;

namespace portfolio_api.Data;

public class ApplicationDbContext : DbContext
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
        : base(options)
    {
    }

    // Agrega tus DbSets aquí, por ejemplo:
    public DbSet<Project> Projects { get; set; }
    public DbSet<User> Users { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // llave compuesta para User (Id, Username)
        modelBuilder.Entity<User>()
            .HasKey(u => new { u.Id, u.Username });

        modelBuilder.Entity<Project>()
            .HasOne(p => p.User)
            .WithMany(p => p.Projects)
            .HasForeignKey(p => new { p.UserId, p.UserName })
            .OnDelete(DeleteBehavior.Cascade);
    }
}

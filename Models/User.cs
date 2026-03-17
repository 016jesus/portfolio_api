using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace portfolio_api.Models
{
    [Index(nameof(Email), IsUnique = true)]
    [Index(nameof(Username), IsUnique = true)]
    public class User
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public Guid Id { get; set; } = Guid.NewGuid();

        [Required]
        public Guid TenantId { get; set; }

        [Required]
        public required string Username { get; set; }

        public string Provider { get; set; } = "local";

        public string? Password { get; set; }

        [Required]
        public required string Email { get; set; }

        [Required]
        public required string Name { get; set; }

        public string? DisplayName { get; set; }

        public string? AvatarUrl { get; set; }

        public string? Bio { get; set; }

        public string? Website { get; set; }

        public string? Location { get; set; }

        public string? GithubUsername { get; set; }

        // Token de acceso OAuth para hacer llamadas a la API de GitHub/Google
        // Se almacena de forma segura, no se expone en DTOs públicos
        public string? OAuthAccessToken { get; set; }

        public string? OAuthRefreshToken { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public ICollection<Project> Projects { get; set; } = new List<Project>();

        public ICollection<Skill> Skills { get; set; } = new List<Skill>();
    }
}

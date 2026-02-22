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

        public string? Password { get; set; } = string.Empty;


        [Required]
        public required string Email { get; set; }
        [Required]
        public required string Name { get; set; }
        
        public ICollection<Project> Projects { get; set; } = new List<Project>();

        public ICollection<Skill> Skills { get; set; } = new List<Skill>();
    }
}

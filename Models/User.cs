using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace portfolio_api.Models
{
    public class User
    {
        public Guid Id { get; set; } = Guid.NewGuid();

        [Required]
        public required string Username { get; set; }
        [Required]
        public required string Name { get; set; }
        
        public ICollection<Project> Projects { get; set; } = new List<Project>();
    }
}

using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
namespace portfolio_api.Models
{
    public class Skill
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public Guid Id { get; set; } = Guid.NewGuid();

        [Required]
        public Guid TenantId { get; set; }

        [Required]
        [MaxLength(100)]
        public string Name { get; set; } = string.Empty;
        [MaxLength(1500)]
        public string Description { get; set; } = string.Empty;
        [Required]
        public Guid UserId { get; set; }

        //propiedad de navegacion (mapea la tabla que se referencia)
        public User User { get; set; } = null!;


    }
}

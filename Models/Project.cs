using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace portfolio_api.Models;

public class Project
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public Guid Id { get; set; } = Guid.NewGuid();

    [Required]
    public Guid TenantId { get; set; }

    [Required]
    [MaxLength(200)]
    public string Title { get; set; } = string.Empty;

    [MaxLength(1000)]
    public string? Description { get; set; }

    [MaxLength(500)]
    public string? Url { get; set; }

    public string? image { get; set; }
   
    
    [Required]
    public DateTime CreationDate { get; set; } 

    public DateTime? EndDate { get; set; }

    // GitHub integration fields
    public long? GitHubRepoId { get; set; }

    [MaxLength(300)]
    public string? GitHubRepoName { get; set; }

    [MaxLength(100)]
    public string? Role { get; set; }

    public DateTime? StartDate { get; set; }

    public bool IsPinned { get; set; } = false;

    public bool IsVisible { get; set; } = true;

    public int DisplayOrder { get; set; } = 0;

    //clave foranea a User
    [Required]
    public Guid UserId { get; set; }

    //propiedad de navegacion (mapea la tabla que se referencia)
    public User User { get; set; } = null!;
    public ICollection<Technology> Technologies { get; set; } = new List<Technology>();

}

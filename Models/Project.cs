using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace portfolio_api.Models;

public class Project
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public Guid Id { get; set; } = Guid.NewGuid();

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


    //clave foranea a User
    [Required]
    public Guid UserId { get; set; }

    [Required]
    public string UserName { get; set; } = string.Empty;

    //propiedad de navegacion (mapea la tabla que se referencia)
    public User User { get; set; } = null!;

}

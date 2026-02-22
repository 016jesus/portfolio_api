using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using portfolio_api.Data;
using portfolio_api.Models;
using portfolio_api.DTOs;

namespace portfolio_api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class ProjectsController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<ProjectsController> _logger;

        public ProjectsController(ApplicationDbContext context, ILogger<ProjectsController> logger)
        {
            _context = context;
            _logger = logger;
        }

        /// <summary>
        /// Obtener todos los proyectos con información del usuario
        /// </summary>
        [HttpGet]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<ActionResult<IEnumerable<ProjectDto>>> GetProjects([FromQuery] string username)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(username))
                    return BadRequest("El username es requerido");

                var user = await _context.Users.FirstOrDefaultAsync(u => u.Username == username);
                if (user == null)
                    return NotFound("Usuario no encontrado");

                var projects = await _context.Projects
                    .Where(p => p.UserId == user.Id)
                    .Include(p => p.User)
                    .ToListAsync();

                return Ok(projects.Select(p => MapToDto(p)).ToList());
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al obtener proyectos");
                return StatusCode(StatusCodes.Status500InternalServerError, "Error al obtener proyectos");
            }
        }

        /// <summary>
        /// Obtener un proyecto específico por ID
        /// </summary>
        [HttpGet("{id}")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<ActionResult<ProjectDto>> GetProject(Guid id, [FromQuery] string username)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(username))
                    return BadRequest("El username es requerido");

                var user = await _context.Users.FirstOrDefaultAsync(u => u.Username == username);
                if (user == null)
                    return NotFound("Usuario no encontrado");

                var project = await _context.Projects
                    .Include(p => p.User)
                    .FirstOrDefaultAsync(p => p.Id == id && p.UserId == user.Id);

                if (project == null)
                    return NotFound($"Proyecto con ID {id} no encontrado");

                return Ok(MapToDto(project));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al obtener proyecto {ProjectId}", id);
                return StatusCode(StatusCodes.Status500InternalServerError, "Error al obtener proyecto");
            }
        }

        /// <summary>
        /// Obtener todos los proyectos de un usuario específico
        /// </summary>
        [HttpGet("user/{userId}")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<ActionResult<IEnumerable<ProjectDto>>> GetProjectsByUser(Guid userId, string username)
        {
            try
            {
                var user = await _context.Users
                    .FirstOrDefaultAsync(u => u.Id == userId && u.Username == username);

                if (user == null)
                    return NotFound("Usuario no encontrado");

                var projects = await _context.Projects
                    .Where(p => p.UserId == userId)
                    .Include(p => p.User)
                    .ToListAsync();

                return Ok(projects.Select(p => MapToDto(p)).ToList());
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al obtener proyectos del usuario {UserId}", userId);
                return StatusCode(StatusCodes.Status500InternalServerError, "Error al obtener proyectos");
            }
        }

        /// <summary>
        /// Crear un nuevo proyecto
        /// </summary>
        [HttpPost]
        [Authorize]
        [ProducesResponseType(StatusCodes.Status201Created)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<ActionResult<ProjectDto>> CreateProject([FromBody] CreateProjectDto createProjectDto)
        {
            try
            {
                if (!ModelState.IsValid)
                    return BadRequest(ModelState);

                // Verificar que el usuario existe
                var user = await _context.Users
                    .FirstOrDefaultAsync(u => u.Id == createProjectDto.UserId && u.Username == createProjectDto.UserName);

                if (user == null)
                    return NotFound("Usuario no encontrado");

                var project = new Project
                {
                    Id = Guid.NewGuid(),
                    Title = createProjectDto.Title,
                    Description = createProjectDto.Description,
                    Url = createProjectDto.Url,
                    image = createProjectDto.Image,
                    CreationDate = DateTime.UtcNow,
                    UserId = createProjectDto.UserId,
                    User = user
                };

                _context.Projects.Add(project);
                await _context.SaveChangesAsync();

                _logger.LogInformation("Proyecto creado: {ProjectId} para usuario {UserId}", project.Id, createProjectDto.UserId);

                return CreatedAtAction(nameof(GetProject), new { id = project.Id }, MapToDto(project));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al crear proyecto");
                return StatusCode(StatusCodes.Status500InternalServerError, "Error al crear proyecto");
            }
        }

        /// <summary>
        /// Actualizar un proyecto existente
        /// </summary>
        [HttpPut("{id}")]
        [Authorize]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<ActionResult<ProjectDto>> UpdateProject(Guid id, [FromBody] UpdateProjectDto updateProjectDto)
        {
            try
            {
                var project = await _context.Projects.FindAsync(id);

                if (project == null)
                    return NotFound($"Proyecto con ID {id} no encontrado");

                project.Title = updateProjectDto.Title;
                project.Description = updateProjectDto.Description;
                project.Url = updateProjectDto.Url;
                project.image = updateProjectDto.Image;
                project.EndDate = updateProjectDto.EndDate;

                _context.Projects.Update(project);
                await _context.SaveChangesAsync();

                _logger.LogInformation("Proyecto actualizado: {ProjectId}", id);

                return Ok(MapToDto(project));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al actualizar proyecto {ProjectId}", id);
                return StatusCode(StatusCodes.Status500InternalServerError, "Error al actualizar proyecto");
            }
        }

        /// <summary>
        /// Eliminar un proyecto
        /// </summary>
        [HttpDelete("{id}")]
        [Authorize]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> DeleteProject(Guid id)
        {
            try
            {
                var project = await _context.Projects.FindAsync(id);

                if (project == null)
                    return NotFound($"Proyecto con ID {id} no encontrado");

                _context.Projects.Remove(project);
                await _context.SaveChangesAsync();

                _logger.LogInformation("Proyecto eliminado: {ProjectId}", id);

                return NoContent();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al eliminar proyecto {ProjectId}", id);
                return StatusCode(StatusCodes.Status500InternalServerError, "Error al eliminar proyecto");
            }
        }

        private static ProjectDto MapToDto(Project project)
        {
            return new ProjectDto
            {
                Id = project.Id,
                Title = project.Title,
                Description = project.Description,
                Url = project.Url,
                Image = project.image,
                CreationDate = project.CreationDate,
                EndDate = project.EndDate,
                UserId = project.UserId,
                UserName_Display = project.User?.Name ?? "Desconocido"
            };
        }
    }
}

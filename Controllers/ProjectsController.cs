using System.Security.Claims;
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
        private readonly ITenantProvider _tenantProvider;

        public ProjectsController(ApplicationDbContext context, ILogger<ProjectsController> logger, ITenantProvider tenantProvider)
        {
            _context = context;
            _logger = logger;
            _tenantProvider = tenantProvider;
        }

        /// <summary>
        /// Obtener todos los proyectos con información del usuario
        /// </summary>
        [HttpGet]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<ActionResult<IEnumerable<ProjectDto>>> GetProjects([FromQuery] string username, [FromQuery] string tenantId)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(tenantId))
                    return BadRequest("TenantId requerido");

                if (string.IsNullOrWhiteSpace(username))
                    return BadRequest("El username es requerido");

                var user = await _context.Users.FirstOrDefaultAsync(u => u.Username == username);
                if (user == null)
                    return NotFound("Usuario no encontrado");

                var projects = await _context.Projects
                    .Where(p => p.UserId == user.Id)
                    .Include(p => p.User)
                    .Include(p => p.Technologies)
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
                if (_tenantProvider.TenantId == null)
                    return BadRequest("TenantId requerido");

                if (string.IsNullOrWhiteSpace(username))
                    return BadRequest("El username es requerido");

                var user = await _context.Users.FirstOrDefaultAsync(u => u.Username == username);
                if (user == null)
                    return NotFound("Usuario no encontrado");

                var project = await _context.Projects
                    .Include(p => p.User)
                    .Include(p => p.Technologies)
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
        public async Task<ActionResult<IEnumerable<ProjectDto>>> GetProjectsByUser(string username)
        {
            try
            {
                var user = await _context.Users
                    .FirstOrDefaultAsync(u => u.Username == username);

                if (user == null)
                    return NotFound("Usuario no encontrado");

                var userId = user.Id;
                Console.WriteLine(userId);
                var projects = await _context.Projects
                    .Where(p => p.UserId == userId)
                    .Include(p => p.User)
                    .Include(p => p.Technologies)
                    .ToListAsync();

                return Ok(projects.Select(p => MapToDto(p)).ToList());
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al obtener proyectos del usuario {username}", username);
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
                if (_tenantProvider.TenantId == null)
                    return BadRequest("TenantId requerido");

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
                    Role = createProjectDto.Role,
                    StartDate = createProjectDto.StartDate,
                    IsPinned = createProjectDto.IsPinned,
                    IsVisible = createProjectDto.IsVisible,
                    DisplayOrder = createProjectDto.DisplayOrder,
                    UserId = createProjectDto.UserId,
                    User = user,
                    TenantId = _tenantProvider.TenantId.Value
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
        /// Crear un proyecto a partir de un repositorio de GitHub
        /// </summary>
        [HttpPost("from-repo")]
        [Authorize]
        [ProducesResponseType(StatusCodes.Status201Created)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status409Conflict)]
        public async Task<ActionResult<ProjectDto>> CreateProjectFromRepo([FromBody] CreateProjectFromRepoDto dto)
        {
            try
            {
                var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (!Guid.TryParse(userIdClaim, out var userId))
                    return Unauthorized();

                var user = await _context.Users
                    .IgnoreQueryFilters()
                    .FirstOrDefaultAsync(u => u.Id == userId);

                if (user == null)
                    return Unauthorized();

                // Check if a project with same GitHubRepoId already exists for this user
                var existing = await _context.Projects
                    .IgnoreQueryFilters()
                    .FirstOrDefaultAsync(p => p.UserId == userId && p.GitHubRepoId == dto.GitHubRepoId);

                if (existing != null)
                    return Conflict("Ya existe un proyecto con este repositorio de GitHub");

                var project = new Project
                {
                    Id = Guid.NewGuid(),
                    GitHubRepoId = dto.GitHubRepoId,
                    GitHubRepoName = dto.GitHubRepoName,
                    Title = dto.Title ?? dto.GitHubRepoName.Split('/').Last(),
                    Description = dto.Description,
                    Role = dto.Role,
                    image = dto.Image,
                    CreationDate = DateTime.UtcNow,
                    IsVisible = true,
                    UserId = userId,
                    User = user,
                    TenantId = user.TenantId
                };

                _context.Projects.Add(project);
                await _context.SaveChangesAsync();

                _logger.LogInformation("Proyecto creado desde repo: {ProjectId} para usuario {UserId}", project.Id, userId);

                return CreatedAtAction(nameof(GetProject), new { id = project.Id }, MapToDto(project));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al crear proyecto desde repositorio");
                return StatusCode(StatusCodes.Status500InternalServerError, "Error al crear proyecto desde repositorio");
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
                if (_tenantProvider.TenantId == null)
                    return BadRequest("TenantId requerido");

                var project = await _context.Projects
                    .Include(p => p.Technologies)
                    .FirstOrDefaultAsync(p => p.Id == id);

                if (project == null)
                    return NotFound($"Proyecto con ID {id} no encontrado");

                project.Title = updateProjectDto.Title;
                project.Description = updateProjectDto.Description;
                project.Url = updateProjectDto.Url;
                project.image = updateProjectDto.Image;
                project.EndDate = updateProjectDto.EndDate;

                if (updateProjectDto.Role != null)
                    project.Role = updateProjectDto.Role;
                if (updateProjectDto.StartDate != null)
                    project.StartDate = updateProjectDto.StartDate;
                if (updateProjectDto.IsPinned != null)
                    project.IsPinned = updateProjectDto.IsPinned.Value;
                if (updateProjectDto.IsVisible != null)
                    project.IsVisible = updateProjectDto.IsVisible.Value;
                if (updateProjectDto.DisplayOrder != null)
                    project.DisplayOrder = updateProjectDto.DisplayOrder.Value;

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
        /// Actualizar visibilidad, pin y orden de un proyecto
        /// </summary>
        [HttpPut("{id}/visibility")]
        [Authorize]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        public async Task<ActionResult<ProjectDto>> UpdateProjectVisibility(Guid id, [FromBody] ProjectVisibilityDto dto)
        {
            try
            {
                var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (!Guid.TryParse(userIdClaim, out var userId))
                    return Unauthorized();

                var project = await _context.Projects
                    .IgnoreQueryFilters()
                    .Include(p => p.Technologies)
                    .FirstOrDefaultAsync(p => p.Id == id);

                if (project == null)
                    return NotFound($"Proyecto con ID {id} no encontrado");

                if (project.UserId != userId)
                    return Forbid();

                project.IsVisible = dto.IsVisible;
                project.IsPinned = dto.IsPinned;
                project.DisplayOrder = dto.DisplayOrder;

                _context.Projects.Update(project);
                await _context.SaveChangesAsync();

                _logger.LogInformation("Visibilidad actualizada para proyecto: {ProjectId}", id);

                return Ok(MapToDto(project));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al actualizar visibilidad del proyecto {ProjectId}", id);
                return StatusCode(StatusCodes.Status500InternalServerError, "Error al actualizar visibilidad del proyecto");
            }
        }

        /// <summary>
        /// Reordenar múltiples proyectos
        /// </summary>
        [HttpPut("reorder")]
        [Authorize]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        public async Task<IActionResult> ReorderProjects([FromBody] List<ReorderProjectDto> reorderList)
        {
            try
            {
                var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (!Guid.TryParse(userIdClaim, out var userId))
                    return Unauthorized();

                var projectIds = reorderList.Select(r => r.Id).ToList();
                var projects = await _context.Projects
                    .IgnoreQueryFilters()
                    .Where(p => projectIds.Contains(p.Id))
                    .ToListAsync();

                // Verify all projects belong to authenticated user
                if (projects.Any(p => p.UserId != userId))
                    return Forbid();

                if (projects.Count != reorderList.Count)
                    return BadRequest("Algunos proyectos no fueron encontrados");

                foreach (var item in reorderList)
                {
                    var project = projects.First(p => p.Id == item.Id);
                    project.DisplayOrder = item.DisplayOrder;
                }

                await _context.SaveChangesAsync();

                _logger.LogInformation("Proyectos reordenados para usuario {UserId}", userId);

                return Ok(new { message = "Proyectos reordenados correctamente" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al reordenar proyectos");
                return StatusCode(StatusCodes.Status500InternalServerError, "Error al reordenar proyectos");
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
                if (_tenantProvider.TenantId == null)
                    return BadRequest("TenantId requerido");

                var project = await _context.Projects.FirstOrDefaultAsync(p => p.Id == id);

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
                UserName_Display = project.User?.Name ?? "Desconocido",
                GitHubRepoId = project.GitHubRepoId,
                GitHubRepoName = project.GitHubRepoName,
                Role = project.Role,
                StartDate = project.StartDate,
                IsPinned = project.IsPinned,
                IsVisible = project.IsVisible,
                DisplayOrder = project.DisplayOrder,
                Technologies = project.Technologies?.Select(t => t.Title).ToList() ?? new List<string>()
            };
        }
    }
}

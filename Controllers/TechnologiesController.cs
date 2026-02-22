using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using portfolio_api.Data;
using portfolio_api.DTOs;
using portfolio_api.Models;

namespace portfolio_api.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class TechnologiesController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<TechnologiesController> _logger;

        public TechnologiesController(ApplicationDbContext context, ILogger<TechnologiesController> logger)
        {
            _context = context;
            _logger = logger;
        }

        /// <summary>
        /// Obtener todas las tecnologías
        /// </summary>
        [HttpGet]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<ActionResult<IEnumerable<TechnologyDto>>> GetTechnologies([FromQuery] string username)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(username))
                    return BadRequest("El username es requerido");

                var user = await _context.Users.FirstOrDefaultAsync(u => u.Username == username);
                if (user == null)
                    return NotFound("Usuario no encontrado");

                var technologies = await _context.Technologies
                    .Where(t => t.Projects.Any(p => p.UserId == user.Id))
                    .ToListAsync();
                return Ok(technologies.Select(MapToDto).ToList());
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al obtener tecnologías");
                return StatusCode(StatusCodes.Status500InternalServerError, "Error al obtener tecnologías");
            }
        }

        /// <summary>
        /// Obtener una tecnología por ID
        /// </summary>
        [HttpGet("{id}")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<ActionResult<TechnologyDto>> GetTechnology(Guid id, [FromQuery] string username)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(username))
                    return BadRequest("El username es requerido");

                var user = await _context.Users.FirstOrDefaultAsync(u => u.Username == username);
                if (user == null)
                    return NotFound("Usuario no encontrado");

                var technology = await _context.Technologies
                    .FirstOrDefaultAsync(t => t.Id == id && t.Projects.Any(p => p.UserId == user.Id));

                if (technology == null)
                    return NotFound($"Tecnología con ID {id} no encontrada");

                return Ok(MapToDto(technology));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al obtener tecnología {TechnologyId}", id);
                return StatusCode(StatusCodes.Status500InternalServerError, "Error al obtener tecnología");
            }
        }

        /// <summary>
        /// Crear una nueva tecnología
        /// </summary>
        [HttpPost]
        [Authorize]
        [ProducesResponseType(StatusCodes.Status201Created)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<ActionResult<TechnologyDto>> CreateTechnology([FromBody] CreateTechnologyDto createTechnologyDto)
        {
            try
            {
                if (!ModelState.IsValid)
                    return BadRequest(ModelState);

                var technology = new Technology
                {
                    Id = Guid.NewGuid(),
                    Title = createTechnologyDto.Title,
                    Icon = createTechnologyDto.Icon ?? string.Empty
                };

                _context.Technologies.Add(technology);
                await _context.SaveChangesAsync();

                _logger.LogInformation("Tecnología creada: {TechnologyId}", technology.Id);

                return CreatedAtAction(nameof(GetTechnology), new { id = technology.Id }, MapToDto(technology));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al crear tecnología");
                return StatusCode(StatusCodes.Status500InternalServerError, "Error al crear tecnología");
            }
        }

        /// <summary>
        /// Actualizar una tecnología existente
        /// </summary>
        [HttpPut("{id}")]
        [Authorize]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<ActionResult<TechnologyDto>> UpdateTechnology(Guid id, [FromBody] UpdateTechnologyDto updateTechnologyDto)
        {
            try
            {
                var technology = await _context.Technologies.FindAsync(id);

                if (technology == null)
                    return NotFound($"Tecnología con ID {id} no encontrada");

                technology.Title = updateTechnologyDto.Title;
                technology.Icon = updateTechnologyDto.Icon ?? string.Empty;

                _context.Technologies.Update(technology);
                await _context.SaveChangesAsync();

                _logger.LogInformation("Tecnología actualizada: {TechnologyId}", id);

                return Ok(MapToDto(technology));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al actualizar tecnología {TechnologyId}", id);
                return StatusCode(StatusCodes.Status500InternalServerError, "Error al actualizar tecnología");
            }
        }

        /// <summary>
        /// Eliminar una tecnología
        /// </summary>
        [HttpDelete("{id}")]
        [Authorize]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> DeleteTechnology(Guid id)
        {
            try
            {
                var technology = await _context.Technologies.FindAsync(id);

                if (technology == null)
                    return NotFound($"Tecnología con ID {id} no encontrada");

                _context.Technologies.Remove(technology);
                await _context.SaveChangesAsync();

                _logger.LogInformation("Tecnología eliminada: {TechnologyId}", id);

                return NoContent();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al eliminar tecnología {TechnologyId}", id);
                return StatusCode(StatusCodes.Status500InternalServerError, "Error al eliminar tecnología");
            }
        }

        private static TechnologyDto MapToDto(Technology technology)
        {
            return new TechnologyDto
            {
                Id = technology.Id,
                Title = technology.Title,
                Icon = technology.Icon
            };
        }
    }
}

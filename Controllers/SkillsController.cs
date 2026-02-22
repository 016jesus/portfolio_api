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
    public class SkillsController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<SkillsController> _logger;

        public SkillsController(ApplicationDbContext context, ILogger<SkillsController> logger)
        {
            _context = context;
            _logger = logger;
        }

        /// <summary>
        /// Obtener todas las skills por usuario
        /// </summary>
        [HttpGet]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<ActionResult<IEnumerable<SkillDto>>> GetSkills([FromQuery] string username)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(username))
                    return BadRequest("El username es requerido");

                var user = await _context.Users.FirstOrDefaultAsync(u => u.Username == username);
                if (user == null)
                    return NotFound("Usuario no encontrado");

                var skills = await _context.Skills
                    .Where(s => s.UserId == user.Id)
                    .ToListAsync();

                return Ok(skills.Select(MapToDto).ToList());
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al obtener skills");
                return StatusCode(StatusCodes.Status500InternalServerError, "Error al obtener skills");
            }
        }

        /// <summary>
        /// Obtener una skill por ID
        /// </summary>
        [HttpGet("{id}")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<ActionResult<SkillDto>> GetSkill(Guid id, [FromQuery] string username)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(username))
                    return BadRequest("El username es requerido");

                var user = await _context.Users.FirstOrDefaultAsync(u => u.Username == username);
                if (user == null)
                    return NotFound("Usuario no encontrado");

                var skill = await _context.Skills
                    .FirstOrDefaultAsync(s => s.Id == id && s.UserId == user.Id);

                if (skill == null)
                    return NotFound($"Skill con ID {id} no encontrada");

                return Ok(MapToDto(skill));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al obtener skill {SkillId}", id);
                return StatusCode(StatusCodes.Status500InternalServerError, "Error al obtener skill");
            }
        }

        /// <summary>
        /// Crear una nueva skill
        /// </summary>
        [HttpPost]
        [Authorize]
        [ProducesResponseType(StatusCodes.Status201Created)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<ActionResult<SkillDto>> CreateSkill([FromBody] CreateSkillDto createSkillDto)
        {
            try
            {
                if (!ModelState.IsValid)
                    return BadRequest(ModelState);

                var userExists = await _context.Users.AnyAsync(u => u.Id == createSkillDto.UserId);
                if (!userExists)
                    return NotFound("Usuario no encontrado");

                var skill = new Skill
                {
                    Id = Guid.NewGuid(),
                    Name = createSkillDto.Name,
                    Description = createSkillDto.Description ?? string.Empty,
                    UserId = createSkillDto.UserId
                };

                _context.Skills.Add(skill);
                await _context.SaveChangesAsync();

                _logger.LogInformation("Skill creada: {SkillId}", skill.Id);

                return CreatedAtAction(nameof(GetSkill), new { id = skill.Id }, MapToDto(skill));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al crear skill");
                return StatusCode(StatusCodes.Status500InternalServerError, "Error al crear skill");
            }
        }

        /// <summary>
        /// Actualizar una skill existente
        /// </summary>
        [HttpPut("{id}")]
        [Authorize]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<ActionResult<SkillDto>> UpdateSkill(Guid id, [FromBody] UpdateSkillDto updateSkillDto)
        {
            try
            {
                var skill = await _context.Skills.FindAsync(id);

                if (skill == null)
                    return NotFound($"Skill con ID {id} no encontrada");

                skill.Name = updateSkillDto.Name;
                skill.Description = updateSkillDto.Description ?? string.Empty;

                _context.Skills.Update(skill);
                await _context.SaveChangesAsync();

                _logger.LogInformation("Skill actualizada: {SkillId}", id);

                return Ok(MapToDto(skill));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al actualizar skill {SkillId}", id);
                return StatusCode(StatusCodes.Status500InternalServerError, "Error al actualizar skill");
            }
        }

        /// <summary>
        /// Eliminar una skill
        /// </summary>
        [HttpDelete("{id}")]
        [Authorize]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> DeleteSkill(Guid id)
        {
            try
            {
                var skill = await _context.Skills.FindAsync(id);

                if (skill == null)
                    return NotFound($"Skill con ID {id} no encontrada");

                _context.Skills.Remove(skill);
                await _context.SaveChangesAsync();

                _logger.LogInformation("Skill eliminada: {SkillId}", id);

                return NoContent();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al eliminar skill {SkillId}", id);
                return StatusCode(StatusCodes.Status500InternalServerError, "Error al eliminar skill");
            }
        }

        private static SkillDto MapToDto(Skill skill)
        {
            return new SkillDto
            {
                Id = skill.Id,
                Name = skill.Name,
                Description = skill.Description,
                UserId = skill.UserId
            };
        }
    }
}

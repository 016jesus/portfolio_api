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
    public class UsersController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<UsersController> _logger;
        private readonly ITenantProvider _tenantProvider;

        public UsersController(ApplicationDbContext context, ILogger<UsersController> logger, ITenantProvider tenantProvider)
        {
            _context = context;
            _logger = logger;
            _tenantProvider = tenantProvider;
        }

        /// <summary>
        /// Obtener todos los usuarios con sus proyectos
        /// </summary>
        [HttpGet]
        [ProducesResponseType(StatusCodes.Status200OK)]
        public async Task<ActionResult<IEnumerable<UserDto>>> GetUsers()
        {
            try
            {
                if (_tenantProvider.TenantId == null)
                    return BadRequest("TenantId requerido");

                var users = await _context.Users
                    .Include(u => u.Projects)
                    .ToListAsync();

                return Ok(users.Select(u => MapToDto(u)).ToList());
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al obtener usuarios");
                return StatusCode(StatusCodes.Status500InternalServerError, "Error al obtener usuarios");
            }
        }

        /// <summary>
        /// Obtener perfil público por username (sin TenantId en header)
        /// </summary>
        [HttpGet("public/{username}")]
        [AllowAnonymous]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<ActionResult<UserDto>> GetPublicUser(string username)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(username))
                    return BadRequest("El username es requerido");

                var user = await _context.Users
                    .IgnoreQueryFilters()
                    .AsNoTracking()
                    .FirstOrDefaultAsync(u => u.Username == username);

                if (user == null)
                    return NotFound("Usuario no encontrado");

                var projectCount = await _context.Projects
                    .IgnoreQueryFilters()
                    .CountAsync(p => p.UserId == user.Id && p.TenantId == user.TenantId);

                return Ok(MapToDto(user, projectCount));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al obtener usuario público {Username}", username);
                return StatusCode(StatusCodes.Status500InternalServerError, "Error al obtener usuario público");
            }
        }

        /// <summary>
        /// Obtener un usuario específico por ID o username
        /// </summary>
        [HttpGet("{value}")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<ActionResult<UserDto>> GetUser(string value)
        {
            try
            {

                var user = await _context.Users
                    .Include(u => u.Projects)
                    .FirstOrDefaultAsync(u => (u.Id.ToString() == value || u.Username == value));

                if (user == null)
                    return NotFound($"Usuario con ID {value} no encontrado");

                return Ok(MapToDto(user));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al obtener usuario {UserId}", value);
                return StatusCode(StatusCodes.Status500InternalServerError, "Error al obtener usuario");
            }
        }

        /// <summary>
        /// Crear un nuevo usuario
        /// </summary>
        [HttpPost]
        [ProducesResponseType(StatusCodes.Status201Created)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<ActionResult<UserDto>> CreateUser([FromBody] CreateUserDto createUserDto)
        {
            try
            {
                if (!ModelState.IsValid)
                    return BadRequest(ModelState);

                var tenantId = _tenantProvider.TenantId ?? Guid.NewGuid();

                // Verificar si el username ya existe
                var existingUser = await _context.Users
                    .FirstOrDefaultAsync(u => u.Username == createUserDto.Username);

                if (existingUser != null)
                    return BadRequest("El username ya existe");

                var user = new User
                {
                    Id = Guid.NewGuid(),
                    Username = createUserDto.Username,
                    Email = createUserDto.Email,
                    Name = createUserDto.Name,
                    Password = createUserDto.Password,
                    TenantId = tenantId
                };

                _context.Users.Add(user);
                await _context.SaveChangesAsync();

                _logger.LogInformation("Usuario creado: {UserId}", user.Id);

                return CreatedAtAction(nameof(GetUser), new { value = user.Id }, MapToDto(user));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al crear usuario");
                return StatusCode(StatusCodes.Status500InternalServerError, "Error al crear usuario");
            }
        }

        /// <summary>
        /// Actualizar un usuario existente
        /// </summary>
        [HttpPut("{id}")]
        [Authorize]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<ActionResult<UserDto>> UpdateUser(Guid id, [FromBody] UpdateUserDto updateUserDto)
        {
            try
            {
                if (_tenantProvider.TenantId == null)
                    return BadRequest("TenantId requerido");

                var user = await _context.Users.FirstOrDefaultAsync(u => u.Id == id);

                if (user == null)
                    return NotFound($"Usuario con ID {id} no encontrado");

                // Verificar si el nuevo username ya existe (y no es el del usuario actual)
                if (updateUserDto.Username != user.Username)
                {
                    var existingUser = await _context.Users
                        .FirstOrDefaultAsync(u => u.Username == updateUserDto.Username);

                    if (existingUser != null)
                        return BadRequest("El username ya existe");
                }

                user.Username = updateUserDto.Username;
                user.Name = updateUserDto.Name;

                _context.Users.Update(user);
                await _context.SaveChangesAsync();

                _logger.LogInformation("Usuario actualizado: {UserId}", id);

                return Ok(MapToDto(user));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al actualizar usuario {UserId}", id);
                return StatusCode(StatusCodes.Status500InternalServerError, "Error al actualizar usuario");
            }
        }

        /// <summary>
        /// Eliminar un usuario y sus proyectos asociados
        /// </summary>
        [HttpDelete("{id}")]
        [Authorize]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> DeleteUser(Guid id)
        {
            try
            {
                if (_tenantProvider.TenantId == null)
                    return BadRequest("TenantId requerido");

                var user = await _context.Users.FirstOrDefaultAsync(u => u.Id == id);

                if (user == null)
                    return NotFound($"Usuario con ID {id} no encontrado");

                _context.Users.Remove(user);
                await _context.SaveChangesAsync();

                _logger.LogInformation("Usuario eliminado: {UserId}", id);

                return NoContent();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al eliminar usuario {UserId}", id);
                return StatusCode(StatusCodes.Status500InternalServerError, "Error al eliminar usuario");
            }
        }

        private static UserDto MapToDto(User user)
        {
            return MapToDto(user, user.Projects?.Count ?? 0);
        }

        private static UserDto MapToDto(User user, int projectCount)
        {
            return new UserDto
            {
                Id = user.Id,
                TenantId = user.TenantId,
                Username = user.Username,
                Name = user.Name,
                ProjectCount = projectCount
            };
        }
    }

}
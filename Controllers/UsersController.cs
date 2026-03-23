using System.Net.Http.Headers;
using System.Security.Claims;
using System.Text.Json;
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
        private readonly IHttpClientFactory _httpClientFactory;

        public UsersController(ApplicationDbContext context, ILogger<UsersController> logger, ITenantProvider tenantProvider, IHttpClientFactory httpClientFactory)
        {
            _context = context;
            _logger = logger;
            _tenantProvider = tenantProvider;
            _httpClientFactory = httpClientFactory;
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
        /// Obtener portafolio público de un usuario
        /// </summary>
        [HttpGet("public/{username}/portfolio")]
        [AllowAnonymous]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<ActionResult<IEnumerable<PortfolioProjectDto>>> GetPublicPortfolio(string username)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(username))
                    return BadRequest("El username es requerido");

                // 1. Find user by username
                var user = await _context.Users
                    .IgnoreQueryFilters()
                    .AsNoTracking()
                    .FirstOrDefaultAsync(u => u.Username == username);

                if (user == null)
                    return NotFound("Usuario no encontrado");

                // 2. Get visible DB projects for user, include Technologies, ordered by DisplayOrder
                var dbProjects = await _context.Projects
                    .IgnoreQueryFilters()
                    .Where(p => p.UserId == user.Id && p.TenantId == user.TenantId && p.IsVisible)
                    .Include(p => p.Technologies)
                    .OrderBy(p => p.DisplayOrder)
                    .ToListAsync();

                // Map DB projects to PortfolioProjectDto
                var dbProjectDtos = dbProjects.Select(p => new PortfolioProjectDto
                {
                    Id = p.Id,
                    Title = p.Title,
                    Description = p.Description,
                    Url = p.Url,
                    Image = p.image,
                    Role = p.Role,
                    StartDate = p.StartDate,
                    EndDate = p.EndDate,
                    IsPinned = p.IsPinned,
                    IsVisible = p.IsVisible,
                    DisplayOrder = p.DisplayOrder,
                    GitHubRepoId = p.GitHubRepoId,
                    GitHubRepoName = p.GitHubRepoName,
                    Source = p.GitHubRepoId != null ? "github-custom" : "manual",
                    Technologies = p.Technologies?.Select(t => t.Title).ToList() ?? new List<string>()
                }).ToList();

                // 3. If user wants GitHub repos as default and has GitHub OAuth
                var githubRepoDtos = new List<PortfolioProjectDto>();
                if (user.ShowGitHubReposAsDefault && user.Provider == "github" && !string.IsNullOrWhiteSpace(user.OAuthAccessToken))
                {
                    try
                    {
                        var httpClient = _httpClientFactory.CreateClient();
                        httpClient.DefaultRequestHeaders.UserAgent.ParseAdd("portfolio-api");
                        httpClient.DefaultRequestHeaders.Authorization =
                            new AuthenticationHeaderValue("Bearer", user.OAuthAccessToken);
                        httpClient.DefaultRequestHeaders.Accept.Add(
                            new MediaTypeWithQualityHeaderValue("application/vnd.github+json"));

                        var response = await httpClient.GetAsync(
                            "https://api.github.com/user/repos?sort=updated&per_page=100&page=1");

                        if (response.IsSuccessStatusCode)
                        {
                            var repos = await response.Content.ReadFromJsonAsync<JsonElement[]>();

                            // Get IDs of DB projects that came from GitHub
                            var dbRepoIds = dbProjects
                                .Where(p => p.GitHubRepoId != null)
                                .Select(p => p.GitHubRepoId!.Value)
                                .ToHashSet();

                            // Parse hidden repo IDs
                            var hiddenRepoIds = new HashSet<long>();
                            try
                            {
                                var hiddenIds = JsonSerializer.Deserialize<long[]>(user.HiddenRepoIds ?? "[]");
                                if (hiddenIds != null)
                                    hiddenRepoIds = hiddenIds.ToHashSet();
                            }
                            catch
                            {
                                // If parsing fails, ignore hidden repos
                            }

                            if (repos != null)
                            {
                                foreach (var r in repos)
                                {
                                    var repoId = r.TryGetProperty("id", out var id) ? id.GetInt64() : 0;

                                    // Exclude repos already in DB and hidden repos
                                    if (dbRepoIds.Contains(repoId) || hiddenRepoIds.Contains(repoId))
                                        continue;

                                    githubRepoDtos.Add(new PortfolioProjectDto
                                    {
                                        Id = null,
                                        Title = r.TryGetProperty("name", out var name) ? name.GetString() ?? "" : "",
                                        Description = r.TryGetProperty("description", out var desc) ? desc.GetString() : null,
                                        Url = r.TryGetProperty("html_url", out var url) ? url.GetString() : null,
                                        GitHubRepoId = repoId,
                                        GitHubRepoName = r.TryGetProperty("full_name", out var fn) ? fn.GetString() : null,
                                        Source = "github",
                                        Language = r.TryGetProperty("language", out var lang) ? lang.GetString() : null,
                                        Stars = r.TryGetProperty("stargazers_count", out var stars) ? stars.GetInt32() : 0,
                                        IsVisible = true
                                    });
                                }
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        // 6. If GitHub API fails, just return DB projects (no error to visitor)
                        _logger.LogWarning(ex, "Error al obtener repos de GitHub para portafolio público de {Username}", username);
                    }
                }

                // 5. Merge: pinned projects first -> remaining DB projects by DisplayOrder -> GitHub default repos
                var pinned = dbProjectDtos.Where(p => p.IsPinned).ToList();
                var remainingDb = dbProjectDtos.Where(p => !p.IsPinned).ToList();

                var result = new List<PortfolioProjectDto>();
                result.AddRange(pinned);
                result.AddRange(remainingDb);
                result.AddRange(githubRepoDtos);

                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al obtener portafolio público de {Username}", username);
                return StatusCode(StatusCodes.Status500InternalServerError, "Error al obtener portafolio público");
            }
        }

        /// <summary>
        /// Agregar un repositorio a la lista de ocultos
        /// </summary>
        [HttpPost("me/hidden-repos")]
        [Authorize]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        public async Task<IActionResult> AddHiddenRepo([FromBody] HiddenRepoDto dto)
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

                var hiddenIds = JsonSerializer.Deserialize<List<long>>(user.HiddenRepoIds ?? "[]") ?? new List<long>();

                if (!hiddenIds.Contains(dto.RepoId))
                {
                    hiddenIds.Add(dto.RepoId);
                    user.HiddenRepoIds = JsonSerializer.Serialize(hiddenIds);
                    _context.Users.Update(user);
                    await _context.SaveChangesAsync();
                }

                _logger.LogInformation("Repo {RepoId} ocultado para usuario {UserId}", dto.RepoId, userId);

                return Ok(new { hiddenRepoIds = hiddenIds });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al ocultar repositorio");
                return StatusCode(StatusCodes.Status500InternalServerError, "Error al ocultar repositorio");
            }
        }

        /// <summary>
        /// Eliminar un repositorio de la lista de ocultos
        /// </summary>
        [HttpDelete("me/hidden-repos/{repoId}")]
        [Authorize]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        public async Task<IActionResult> RemoveHiddenRepo(long repoId)
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

                var hiddenIds = JsonSerializer.Deserialize<List<long>>(user.HiddenRepoIds ?? "[]") ?? new List<long>();

                if (hiddenIds.Remove(repoId))
                {
                    user.HiddenRepoIds = JsonSerializer.Serialize(hiddenIds);
                    _context.Users.Update(user);
                    await _context.SaveChangesAsync();
                }

                _logger.LogInformation("Repo {RepoId} mostrado de nuevo para usuario {UserId}", repoId, userId);

                return Ok(new { hiddenRepoIds = hiddenIds });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al mostrar repositorio");
                return StatusCode(StatusCodes.Status500InternalServerError, "Error al mostrar repositorio");
            }
        }

        /// <summary>
        /// Actualizar perfil del usuario autenticado
        /// </summary>
        [HttpPut("{id}/profile")]
        [Authorize]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<ActionResult<UserDto>> UpdateProfile(Guid id, [FromBody] UpdateProfileDto dto)
        {
            try
            {
                var user = await _context.Users
                    .IgnoreQueryFilters()
                    .FirstOrDefaultAsync(u => u.Id == id);

                if (user == null)
                    return NotFound($"Usuario con ID {id} no encontrado");

                if (dto.Username != user.Username)
                {
                    var existingUser = await _context.Users
                        .IgnoreQueryFilters()
                        .FirstOrDefaultAsync(u => u.Username == dto.Username);
                    if (existingUser != null)
                        return BadRequest("El username ya existe");
                }

                user.Username = dto.Username;
                user.Name = dto.Name;
                user.DisplayName = dto.DisplayName;
                user.Bio = dto.Bio;
                user.Website = dto.Website;
                user.Location = dto.Location;
                if (!string.IsNullOrWhiteSpace(dto.AvatarUrl))
                    user.AvatarUrl = dto.AvatarUrl;

                _context.Users.Update(user);
                await _context.SaveChangesAsync();

                _logger.LogInformation("Perfil actualizado: {UserId}", id);
                return Ok(MapToDto(user));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al actualizar perfil {UserId}", id);
                return StatusCode(StatusCodes.Status500InternalServerError, "Error al actualizar perfil");
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

                var hashedPassword = BCrypt.Net.BCrypt.HashPassword(createUserDto.Password);
                var user = new User
                {
                    Id = Guid.NewGuid(),
                    Username = createUserDto.Username,
                    Email = createUserDto.Email,
                    Name = createUserDto.Name,
                    Password = hashedPassword,
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
        public async Task<ActionResult<UserDto>> UpdateUser(Guid id, [FromBody] UpdateProfileDto dto)
        {
            try
            {
                var user = await _context.Users
                    .IgnoreQueryFilters()
                    .FirstOrDefaultAsync(u => u.Id == id);

                if (user == null)
                    return NotFound($"Usuario con ID {id} no encontrado");

                if (dto.Username != user.Username)
                {
                    var existingUser = await _context.Users
                        .IgnoreQueryFilters()
                        .FirstOrDefaultAsync(u => u.Username == dto.Username);
                    if (existingUser != null)
                        return BadRequest("El username ya existe");
                }

                user.Username = dto.Username;
                user.Name = dto.Name;
                user.DisplayName = dto.DisplayName;
                user.Bio = dto.Bio;
                user.Website = dto.Website;
                user.Location = dto.Location;
                if (!string.IsNullOrWhiteSpace(dto.AvatarUrl))
                    user.AvatarUrl = dto.AvatarUrl;

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
                DisplayName = user.DisplayName,
                AvatarUrl = user.AvatarUrl,
                Bio = user.Bio,
                Website = user.Website,
                Location = user.Location,
                GithubUsername = user.GithubUsername,
                Email = user.Email,
                Provider = user.Provider,
                CreatedAt = user.CreatedAt,
                ProjectCount = projectCount
            };
        }
    }

}

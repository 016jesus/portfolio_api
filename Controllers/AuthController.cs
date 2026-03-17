using System.IdentityModel.Tokens.Jwt;
using System.Net.Http.Headers;
using System.Security.Claims;
using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using portfolio_api.Data;
using portfolio_api.DTOs;
using portfolio_api.Models;

namespace portfolio_api.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class AuthController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        private readonly IConfiguration _configuration;
        private readonly ILogger<AuthController> _logger;
        private readonly IHttpClientFactory _httpClientFactory;

        public AuthController(
            ApplicationDbContext context,
            IConfiguration configuration,
            ILogger<AuthController> logger,
            IHttpClientFactory httpClientFactory)
        {
            _context = context;
            _configuration = configuration;
            _logger = logger;
            _httpClientFactory = httpClientFactory;
        }

        // POST /api/Auth/login
        [HttpPost("login")]
        [AllowAnonymous]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        public async Task<ActionResult<LoginResponseDto>> Login([FromBody] LoginRequestDto loginDto)
        {
            try
            {
                if (!ModelState.IsValid)
                    return BadRequest(ModelState);

                var user = await _context.Users
                    .IgnoreQueryFilters()
                    .FirstOrDefaultAsync(u => u.Username == loginDto.Username || u.Email == loginDto.Username);

                if (user == null || string.IsNullOrWhiteSpace(user.Password))
                    return Unauthorized("Credenciales inválidas");

                bool passwordValid = BCrypt.Net.BCrypt.Verify(loginDto.Password, user.Password);
                if (!passwordValid)
                    return Unauthorized("Credenciales inválidas");

                var token = GenerateJwtToken(user);
                var expiresAt = DateTime.UtcNow.AddHours(8);

                return Ok(new LoginResponseDto
                {
                    Token = token,
                    ExpiresAt = expiresAt,
                    User = MapToMeDto(user)
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al iniciar sesión");
                return StatusCode(StatusCodes.Status500InternalServerError, "Error al iniciar sesión");
            }
        }

        // POST /api/Auth/register
        [HttpPost("register")]
        [AllowAnonymous]
        [ProducesResponseType(StatusCodes.Status201Created)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status409Conflict)]
        public async Task<ActionResult<LoginResponseDto>> Register([FromBody] RegisterRequestDto dto)
        {
            try
            {
                if (!ModelState.IsValid)
                    return BadRequest(ModelState);

                if (dto.Password.Length < 8)
                    return BadRequest("La contraseña debe tener al menos 8 caracteres");

                var existing = await _context.Users
                    .IgnoreQueryFilters()
                    .FirstOrDefaultAsync(u => u.Username == dto.Username || u.Email == dto.Email);

                if (existing != null)
                    return Conflict(existing.Username == dto.Username
                        ? "El nombre de usuario ya está en uso"
                        : "El correo electrónico ya está en uso");

                var tenantId = Guid.NewGuid();
                var hashedPassword = BCrypt.Net.BCrypt.HashPassword(dto.Password);

                var user = new User
                {
                    Id = Guid.NewGuid(),
                    TenantId = tenantId,
                    Username = dto.Username,
                    Email = dto.Email,
                    Name = dto.Name,
                    Password = hashedPassword,
                    Provider = "local",
                    CreatedAt = DateTime.UtcNow
                };

                _context.Users.Add(user);
                await _context.SaveChangesAsync();

                var token = GenerateJwtToken(user);

                _logger.LogInformation("Usuario registrado: {UserId}", user.Id);

                return StatusCode(StatusCodes.Status201Created, new LoginResponseDto
                {
                    Token = token,
                    ExpiresAt = DateTime.UtcNow.AddHours(8),
                    User = MapToMeDto(user)
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al registrar usuario");
                return StatusCode(StatusCodes.Status500InternalServerError, "Error al registrar usuario");
            }
        }

        // POST /api/Auth/oauth-login
        [HttpPost("oauth-login")]
        [AllowAnonymous]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        public async Task<ActionResult<LoginResponseDto>> OAuthLogin([FromBody] OAuthLoginRequestDto dto)
        {
            try
            {
                if (!ModelState.IsValid)
                    return BadRequest(ModelState);

                return dto.Provider.ToLower() switch
                {
                    "github" => await HandleGitHubOAuth(dto.Code, dto.RedirectUri),
                    "google" => await HandleGoogleOAuth(dto.Code, dto.RedirectUri),
                    _ => BadRequest("Proveedor OAuth no soportado")
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error en OAuth login con proveedor {Provider}", dto.Provider);
                return StatusCode(StatusCodes.Status500InternalServerError, "Error en autenticación OAuth");
            }
        }

        // GET /api/Auth/me
        [HttpGet("me")]
        [Authorize]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<ActionResult<MeDto>> Me()
        {
            try
            {
                var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (!Guid.TryParse(userIdClaim, out var userId))
                    return Unauthorized();

                var user = await _context.Users
                    .IgnoreQueryFilters()
                    .AsNoTracking()
                    .FirstOrDefaultAsync(u => u.Id == userId);

                if (user == null)
                    return NotFound("Usuario no encontrado");

                return Ok(MapToMeDto(user));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al obtener usuario autenticado");
                return StatusCode(StatusCodes.Status500InternalServerError, "Error al obtener perfil");
            }
        }

        // POST /api/Auth/logout
        [HttpPost("logout")]
        [Authorize]
        [ProducesResponseType(StatusCodes.Status200OK)]
        public IActionResult Logout()
        {
            // JWT es stateless; el cliente elimina el token
            return Ok(new { message = "Sesión cerrada correctamente" });
        }

        // ── Helpers privados ────────────────────────────────────────────

        private async Task<ActionResult<LoginResponseDto>> HandleGitHubOAuth(string code, string? redirectUri)
        {
            var clientId = _configuration["Authentication:GitHub:ClientId"];
            if (string.IsNullOrWhiteSpace(clientId))
                clientId = Environment.GetEnvironmentVariable("GITHUB_CLIENT_ID");

            var clientSecret = _configuration["Authentication:GitHub:ClientSecret"];
            if (string.IsNullOrWhiteSpace(clientSecret))
                clientSecret = Environment.GetEnvironmentVariable("GITHUB_CLIENT_SECRET");

            if (string.IsNullOrWhiteSpace(clientId) || string.IsNullOrWhiteSpace(clientSecret))
                return StatusCode(503, "GitHub OAuth no configurado");

            var httpClient = _httpClientFactory.CreateClient();
            httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
            httpClient.DefaultRequestHeaders.UserAgent.ParseAdd("portfolio-api");

            // Intercambiar code por access_token
            var tokenResponse = await httpClient.PostAsync(
                "https://github.com/login/oauth/access_token",
                new FormUrlEncodedContent(new Dictionary<string, string>
                {
                    ["client_id"] = clientId,
                    ["client_secret"] = clientSecret,
                    ["code"] = code,
                    ["redirect_uri"] = redirectUri ?? string.Empty
                }));

            if (!tokenResponse.IsSuccessStatusCode)
                return Unauthorized("No se pudo obtener el token de GitHub");

            var tokenJson = await tokenResponse.Content.ReadFromJsonAsync<JsonElement>();

            if (tokenJson.TryGetProperty("error", out var ghError))
                return Unauthorized($"Error de GitHub: {ghError.GetString()}");

            if (!tokenJson.TryGetProperty("access_token", out var accessTokenProp))
                return Unauthorized("Token de GitHub inválido");

            var accessToken = accessTokenProp.GetString();
            if (string.IsNullOrWhiteSpace(accessToken))
                return Unauthorized("Token de GitHub inválido");

            // Obtener info del usuario de GitHub
            httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);

            var userResponse = await httpClient.GetAsync("https://api.github.com/user");
            if (!userResponse.IsSuccessStatusCode)
                return Unauthorized("No se pudo obtener información del usuario de GitHub");

            var githubUser = await userResponse.Content.ReadFromJsonAsync<JsonElement>();

            var githubId = githubUser.GetProperty("id").GetInt64().ToString();
            var githubLogin = githubUser.TryGetProperty("login", out var loginProp) ? loginProp.GetString() : null;
            var githubName = githubUser.TryGetProperty("name", out var nameProp) ? nameProp.GetString() : githubLogin;
            var githubAvatar = githubUser.TryGetProperty("avatar_url", out var avatarProp) ? avatarProp.GetString() : null;
            var githubBio = githubUser.TryGetProperty("bio", out var bioProp) ? bioProp.GetString() : null;
            var githubLocation = githubUser.TryGetProperty("location", out var locationProp) ? locationProp.GetString() : null;
            var githubWebsite = githubUser.TryGetProperty("blog", out var blogProp) ? blogProp.GetString() : null;

            // Obtener email si no viene en el perfil
            string? githubEmail = githubUser.TryGetProperty("email", out var emailProp) ? emailProp.GetString() : null;
            if (string.IsNullOrWhiteSpace(githubEmail))
            {
                var emailResponse = await httpClient.GetAsync("https://api.github.com/user/emails");
                if (emailResponse.IsSuccessStatusCode)
                {
                    var emails = await emailResponse.Content.ReadFromJsonAsync<JsonElement[]>();
                    githubEmail = emails?.FirstOrDefault(e =>
                        e.TryGetProperty("primary", out var p) && p.GetBoolean())
                        .TryGetProperty("email", out var ep) == true ? emails!
                        .First(e => e.TryGetProperty("primary", out var p) && p.GetBoolean())
                        .GetProperty("email").GetString() : null;
                }
            }

            return await UpsertOAuthUser(
                providerKey: $"github:{githubId}",
                provider: "github",
                email: githubEmail ?? $"{githubLogin}@github.noemail",
                username: githubLogin ?? $"gh_{githubId}",
                name: githubName ?? githubLogin ?? $"gh_{githubId}",
                avatarUrl: githubAvatar,
                bio: githubBio,
                location: githubLocation,
                website: githubWebsite,
                githubUsername: githubLogin,
                accessToken: accessToken);
        }

        private async Task<ActionResult<LoginResponseDto>> HandleGoogleOAuth(string code, string? redirectUri)
        {
            var clientId = _configuration["Authentication:Google:ClientId"];
            if (string.IsNullOrWhiteSpace(clientId))
                clientId = Environment.GetEnvironmentVariable("GOOGLE_CLIENT_ID");

            var clientSecret = _configuration["Authentication:Google:ClientSecret"];
            if (string.IsNullOrWhiteSpace(clientSecret))
                clientSecret = Environment.GetEnvironmentVariable("GOOGLE_CLIENT_SECRET");

            if (string.IsNullOrWhiteSpace(clientId) || string.IsNullOrWhiteSpace(clientSecret))
                return StatusCode(503, "Google OAuth no configurado");

            var httpClient = _httpClientFactory.CreateClient();

            // Intercambiar code por access_token
            var tokenResponse = await httpClient.PostAsync(
                "https://oauth2.googleapis.com/token",
                new FormUrlEncodedContent(new Dictionary<string, string>
                {
                    ["client_id"] = clientId,
                    ["client_secret"] = clientSecret,
                    ["code"] = code,
                    ["grant_type"] = "authorization_code",
                    ["redirect_uri"] = redirectUri ?? string.Empty
                }));

            if (!tokenResponse.IsSuccessStatusCode)
                return Unauthorized("No se pudo obtener el token de Google");

            var tokenJson = await tokenResponse.Content.ReadFromJsonAsync<JsonElement>();
            var accessToken = tokenJson.GetProperty("access_token").GetString();

            if (string.IsNullOrWhiteSpace(accessToken))
                return Unauthorized("Token de Google inválido");

            // Obtener info del usuario
            var userResponse = await httpClient.GetAsync(
                $"https://www.googleapis.com/oauth2/v2/userinfo?access_token={accessToken}");

            if (!userResponse.IsSuccessStatusCode)
                return Unauthorized("No se pudo obtener información del usuario de Google");

            var googleUser = await userResponse.Content.ReadFromJsonAsync<JsonElement>();
            var googleId = googleUser.GetProperty("id").GetString();
            var googleEmail = googleUser.TryGetProperty("email", out var ep) ? ep.GetString() : null;
            var googleName = googleUser.TryGetProperty("name", out var np) ? np.GetString() : googleEmail;
            var googleAvatar = googleUser.TryGetProperty("picture", out var pp) ? pp.GetString() : null;

            if (string.IsNullOrWhiteSpace(googleEmail))
                return BadRequest("No se pudo obtener el correo de Google");

            var username = googleEmail.Split('@')[0].Replace(".", "_").ToLower();

            return await UpsertOAuthUser(
                providerKey: $"google:{googleId}",
                provider: "google",
                email: googleEmail,
                username: username,
                name: googleName ?? username,
                avatarUrl: googleAvatar,
                bio: null,
                location: null,
                website: null,
                githubUsername: null,
                accessToken: accessToken);
        }

        private async Task<ActionResult<LoginResponseDto>> UpsertOAuthUser(
            string providerKey, string provider, string email, string username,
            string name, string? avatarUrl, string? bio, string? location,
            string? website, string? githubUsername, string accessToken)
        {
            // Buscar usuario existente por email (ignorando tenant filter)
            var user = await _context.Users
                .IgnoreQueryFilters()
                .FirstOrDefaultAsync(u => u.Email == email);

            if (user == null)
            {
                // Asegurar username único
                var baseUsername = username;
                var counter = 1;
                while (await _context.Users.IgnoreQueryFilters()
                           .AnyAsync(u => u.Username == username))
                {
                    username = $"{baseUsername}{counter++}";
                }

                user = new User
                {
                    Id = Guid.NewGuid(),
                    TenantId = Guid.NewGuid(),
                    Username = username,
                    Email = email,
                    Name = name,
                    Provider = provider,
                    AvatarUrl = avatarUrl,
                    Bio = bio,
                    Location = location,
                    Website = website,
                    GithubUsername = githubUsername,
                    OAuthAccessToken = accessToken,
                    CreatedAt = DateTime.UtcNow
                };

                _context.Users.Add(user);
            }
            else
            {
                // Actualizar datos que pueden haber cambiado
                user.Provider = provider;
                user.OAuthAccessToken = accessToken;
                if (!string.IsNullOrWhiteSpace(avatarUrl)) user.AvatarUrl = avatarUrl;
                if (!string.IsNullOrWhiteSpace(bio)) user.Bio = bio;
                if (!string.IsNullOrWhiteSpace(location)) user.Location = location;
                if (!string.IsNullOrWhiteSpace(website)) user.Website = website;
                if (!string.IsNullOrWhiteSpace(githubUsername)) user.GithubUsername = githubUsername;
            }

            await _context.SaveChangesAsync();

            var token = GenerateJwtToken(user);
            _logger.LogInformation("OAuth login exitoso para usuario {UserId} via {Provider}", user.Id, provider);

            return Ok(new LoginResponseDto
            {
                Token = token,
                ExpiresAt = DateTime.UtcNow.AddHours(8),
                User = MapToMeDto(user)
            });
        }

        private string GenerateJwtToken(User user)
        {
            var jwtKey = _configuration["Jwt:Key"]
                ?? Environment.GetEnvironmentVariable("JWT_SECRET")
                ?? Environment.GetEnvironmentVariable("API_KEY");

            if (string.IsNullOrWhiteSpace(jwtKey))
                throw new InvalidOperationException("JWT key no configurada");

            var issuer = _configuration["Jwt:Issuer"] ?? "portfolio_api";
            var audience = _configuration["Jwt:Audience"] ?? "portfolio_api";

            var claims = new List<Claim>
            {
                new(ClaimTypes.NameIdentifier, user.Id.ToString()),
                new(ClaimTypes.Name, user.Username),
                new(ClaimTypes.Email, user.Email),
                new("tenantId", user.TenantId.ToString()),
                new("provider", user.Provider)
            };

            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey));
            var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

            var token = new JwtSecurityToken(
                issuer: issuer,
                audience: audience,
                claims: claims,
                expires: DateTime.UtcNow.AddHours(8),
                signingCredentials: creds);

            return new JwtSecurityTokenHandler().WriteToken(token);
        }

        private static MeDto MapToMeDto(User user) => new()
        {
            Id = user.Id,
            TenantId = user.TenantId,
            Username = user.Username,
            Name = user.Name,
            DisplayName = user.DisplayName,
            Email = user.Email,
            AvatarUrl = user.AvatarUrl,
            Bio = user.Bio,
            Website = user.Website,
            Location = user.Location,
            GithubUsername = user.GithubUsername,
            Provider = user.Provider
        };
    }
}

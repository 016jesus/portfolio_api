using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using portfolio_api.Data;
using portfolio_api.DTOs;

namespace portfolio_api.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class AuthController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        private readonly IConfiguration _configuration;
        private readonly ILogger<AuthController> _logger;

        public AuthController(ApplicationDbContext context, IConfiguration configuration, ILogger<AuthController> logger)
        {
            _context = context;
            _configuration = configuration;
            _logger = logger;
        }

        [HttpPost("login")]
        [AllowAnonymous]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        public async Task<ActionResult<LoginResponseDto>> Login([FromBody] LoginRequestDto loginDto)
        {
            try
            {
                var tenantIdHeader = HttpContext.Request.Headers["X-Tenant-Id"].FirstOrDefault();
                if (!Guid.TryParse(tenantIdHeader, out _))
                    return BadRequest("TenantId requerido");

                if (!ModelState.IsValid)
                    return BadRequest(ModelState);

                var user = await _context.Users
                    .FirstOrDefaultAsync(u => u.Username == loginDto.Username);

                if (user == null || string.IsNullOrWhiteSpace(user.Password) || user.Password != loginDto.Password)
                    return Unauthorized("Credenciales inválidas");

                var token = GenerateJwtToken(user);
                var expiresAt = DateTime.UtcNow.AddHours(2);

                return Ok(new LoginResponseDto
                {
                    Token = token,
                    ExpiresAt = expiresAt
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al iniciar sesión");
                return StatusCode(StatusCodes.Status500InternalServerError, "Error al iniciar sesión");
            }
        }

        private string GenerateJwtToken(Models.User user)
        {
            var jwtKey = _configuration["Jwt:Key"] ?? Environment.GetEnvironmentVariable("JWT_SECRET") ?? Environment.GetEnvironmentVariable("API_KEY");
            if (string.IsNullOrWhiteSpace(jwtKey))
                throw new InvalidOperationException("JWT key no configurada");

            var issuer = _configuration["Jwt:Issuer"] ?? "portfolio_api";
            var audience = _configuration["Jwt:Audience"] ?? "portfolio_api";

            var claims = new List<Claim>
            {
                new(ClaimTypes.NameIdentifier, user.Id.ToString()),
                new(ClaimTypes.Name, user.Username),
                new(ClaimTypes.Email, user.Email)
            };

            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey));
            var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

            var token = new JwtSecurityToken(
                issuer: issuer,
                audience: audience,
                claims: claims,
                expires: DateTime.UtcNow.AddHours(2),
                signingCredentials: creds);

            return new JwtSecurityTokenHandler().WriteToken(token);
        }
    }
}

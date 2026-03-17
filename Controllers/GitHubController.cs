using System.Net.Http.Headers;
using System.Security.Claims;
using System.Text.Json;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using portfolio_api.Data;

namespace portfolio_api.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class GitHubController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly ILogger<GitHubController> _logger;

        public GitHubController(
            ApplicationDbContext context,
            IHttpClientFactory httpClientFactory,
            ILogger<GitHubController> logger)
        {
            _context = context;
            _httpClientFactory = httpClientFactory;
            _logger = logger;
        }

        /// <summary>
        /// Obtener los repositorios de GitHub del usuario autenticado
        /// </summary>
        [HttpGet("repos")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status503ServiceUnavailable)]
        public async Task<IActionResult> GetRepos([FromQuery] int page = 1, [FromQuery] int perPage = 30)
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
                    return Unauthorized();

                if (string.IsNullOrWhiteSpace(user.OAuthAccessToken) || user.Provider != "github")
                    return StatusCode(422, new { message = "Debes iniciar sesión con GitHub para ver tus repositorios" });

                var httpClient = _httpClientFactory.CreateClient();
                httpClient.DefaultRequestHeaders.UserAgent.ParseAdd("portfolio-api");
                httpClient.DefaultRequestHeaders.Authorization =
                    new AuthenticationHeaderValue("Bearer", user.OAuthAccessToken);
                httpClient.DefaultRequestHeaders.Accept.Add(
                    new MediaTypeWithQualityHeaderValue("application/vnd.github+json"));

                var response = await httpClient.GetAsync(
                    $"https://api.github.com/user/repos?sort=updated&per_page={Math.Min(perPage, 100)}&page={page}");

                if (response.StatusCode == System.Net.HttpStatusCode.Unauthorized)
                    return StatusCode(422, new { message = "El token de GitHub expiró. Por favor vuelve a iniciar sesión." });

                if (!response.IsSuccessStatusCode)
                    return StatusCode(502, new { message = "Error al obtener repositorios de GitHub" });

                var repos = await response.Content.ReadFromJsonAsync<JsonElement[]>();

                var result = repos?.Select(r => new
                {
                    id = r.TryGetProperty("id", out var id) ? id.GetInt64() : 0,
                    name = r.TryGetProperty("name", out var name) ? name.GetString() : null,
                    fullName = r.TryGetProperty("full_name", out var fn) ? fn.GetString() : null,
                    description = r.TryGetProperty("description", out var desc) ? desc.GetString() : null,
                    url = r.TryGetProperty("html_url", out var url) ? url.GetString() : null,
                    cloneUrl = r.TryGetProperty("clone_url", out var clone) ? clone.GetString() : null,
                    homepage = r.TryGetProperty("homepage", out var hp) ? hp.GetString() : null,
                    language = r.TryGetProperty("language", out var lang) ? lang.GetString() : null,
                    stars = r.TryGetProperty("stargazers_count", out var stars) ? stars.GetInt32() : 0,
                    forks = r.TryGetProperty("forks_count", out var forks) ? forks.GetInt32() : 0,
                    isPrivate = r.TryGetProperty("private", out var priv) && priv.GetBoolean(),
                    isFork = r.TryGetProperty("fork", out var fork) && fork.GetBoolean(),
                    topics = r.TryGetProperty("topics", out var topics)
                        ? topics.EnumerateArray().Select(t => t.GetString()).ToArray()
                        : Array.Empty<string>(),
                    updatedAt = r.TryGetProperty("updated_at", out var updated) ? updated.GetString() : null,
                    createdAt = r.TryGetProperty("created_at", out var created) ? created.GetString() : null,
                }) ?? [];

                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al obtener repos de GitHub para usuario");
                return StatusCode(500, "Error al obtener repositorios");
            }
        }
    }
}

namespace portfolio_api.DTOs
{
    public class LoginRequestDto
    {
        public required string Username { get; set; }
        public required string Password { get; set; }
    }

    public class RegisterRequestDto
    {
        public required string Username { get; set; }
        public required string Email { get; set; }
        public required string Name { get; set; }
        public required string Password { get; set; }
    }

    public class LoginResponseDto
    {
        public string Token { get; set; } = string.Empty;
        public DateTime ExpiresAt { get; set; }
        public MeDto User { get; set; } = null!;
        public bool IsNewUser { get; set; } = false;
    }

    public class OAuthLoginRequestDto
    {
        public required string Provider { get; set; }
        public required string Code { get; set; }
        public string? RedirectUri { get; set; }
    }

    public class MeDto
    {
        public Guid Id { get; set; }
        public Guid TenantId { get; set; }
        public string Username { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string? DisplayName { get; set; }
        public string Email { get; set; } = string.Empty;
        public string? AvatarUrl { get; set; }
        public string? Bio { get; set; }
        public string? Website { get; set; }
        public string? Location { get; set; }
        public string? GithubUsername { get; set; }
        public string Provider { get; set; } = "local";
    }
}

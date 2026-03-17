namespace portfolio_api.DTOs
{
    public class UserDto
    {
        public Guid Id { get; set; }
        public Guid TenantId { get; set; }
        public string Username { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string? DisplayName { get; set; }
        public string? AvatarUrl { get; set; }
        public string? Bio { get; set; }
        public string? Website { get; set; }
        public string? Location { get; set; }
        public string? GithubUsername { get; set; }
        public string? Email { get; set; }
        public string Provider { get; set; } = "local";
        public DateTime CreatedAt { get; set; }
        public int ProjectCount { get; set; }
    }

    public class CreateUserDto
    {
        public required string Username { get; set; }
        public required string Email { get; set; }
        public required string Name { get; set; }
        public required string Password { get; set; }
    }

    public class UpdateProfileDto
    {
        public required string Username { get; set; }
        public required string Name { get; set; }
        public string? DisplayName { get; set; }
        public string? Bio { get; set; }
        public string? Website { get; set; }
        public string? Location { get; set; }
        public string? AvatarUrl { get; set; }
    }
}

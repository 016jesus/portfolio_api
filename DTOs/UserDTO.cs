namespace portfolio_api.DTOs
{
    public class UserDto
    {
        public Guid Id { get; set; }
        public string Username { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public int ProjectCount { get; set; }
    }

    public class CreateUserDto
    {
        public required string Username { get; set; }
        public required string Email { get; set; }
        public required string Name { get; set; }
        public required string Password { get; set; }
    }

    public class UpdateUserDto
    {
        public required string Username { get; set; }
        public required string Name { get; set; }
    }
}

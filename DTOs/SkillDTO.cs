namespace portfolio_api.DTOs
{
    public class SkillDto
    {
        public Guid Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public Guid UserId { get; set; }
    }

    public class CreateSkillDto
    {
        public required string Name { get; set; }
        public string? Description { get; set; }
        public required Guid UserId { get; set; }
    }

    public class UpdateSkillDto
    {
        public required string Name { get; set; }
        public string? Description { get; set; }
    }
}

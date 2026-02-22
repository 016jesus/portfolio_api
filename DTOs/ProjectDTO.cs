namespace portfolio_api.DTOs
{
    public class ProjectDto
    {
        public Guid Id { get; set; }
        public string Title { get; set; } = string.Empty;
        public string? Description { get; set; }
        public string? Url { get; set; }
        public string? Image { get; set; }
        public DateTime CreationDate { get; set; }
        public DateTime? EndDate { get; set; }
        public Guid UserId { get; set; }
        public string UserName { get; set; } = string.Empty;
        public string UserName_Display { get; set; } = string.Empty;
    }

    public class CreateProjectDto
    {
        public required string Title { get; set; }
        public string? Description { get; set; }
        public string? Url { get; set; }
        public string? Image { get; set; }
        public required Guid UserId { get; set; }
        public required string UserName { get; set; }
    }

    public class UpdateProjectDto
    {
        public required string Title { get; set; }
        public string? Description { get; set; }
        public string? Url { get; set; }
        public string? Image { get; set; }
        public DateTime? EndDate { get; set; }
    }
}

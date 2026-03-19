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
        public long? GitHubRepoId { get; set; }
        public string? GitHubRepoName { get; set; }
        public string? Role { get; set; }
        public DateTime? StartDate { get; set; }
        public bool IsPinned { get; set; }
        public bool IsVisible { get; set; }
        public int DisplayOrder { get; set; }
        public List<string> Technologies { get; set; } = new();
    }

    public class CreateProjectDto
    {
        public required string Title { get; set; }
        public string? Description { get; set; }
        public string? Url { get; set; }
        public string? Image { get; set; }
        public required Guid UserId { get; set; }
        public required string UserName { get; set; }
        public string? Role { get; set; }
        public DateTime? StartDate { get; set; }
        public bool IsPinned { get; set; } = false;
        public bool IsVisible { get; set; } = true;
        public int DisplayOrder { get; set; } = 0;
        public List<Guid>? TechnologyIds { get; set; }
    }

    public class UpdateProjectDto
    {
        public required string Title { get; set; }
        public string? Description { get; set; }
        public string? Url { get; set; }
        public string? Image { get; set; }
        public DateTime? EndDate { get; set; }
        public string? Role { get; set; }
        public DateTime? StartDate { get; set; }
        public bool? IsPinned { get; set; }
        public bool? IsVisible { get; set; }
        public int? DisplayOrder { get; set; }
    }
}

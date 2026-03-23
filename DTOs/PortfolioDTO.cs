namespace portfolio_api.DTOs
{
    // Unified portfolio project for public display
    public class PortfolioProjectDto
    {
        public Guid? Id { get; set; } // null for GitHub-only repos
        public string Title { get; set; } = string.Empty;
        public string? Description { get; set; }
        public string? Url { get; set; }
        public string? Image { get; set; }
        public string? Role { get; set; }
        public DateTime? StartDate { get; set; }
        public DateTime? EndDate { get; set; }
        public bool IsPinned { get; set; }
        public bool IsVisible { get; set; }
        public int DisplayOrder { get; set; }
        public long? GitHubRepoId { get; set; }
        public string? GitHubRepoName { get; set; }
        public string Source { get; set; } = "manual"; // "manual" | "github" | "github-custom"
        public List<string> Technologies { get; set; } = new();
        public string? Language { get; set; } // from GitHub API
        public int? Stars { get; set; } // from GitHub API
    }

    // For converting a GitHub repo into a DB project
    public class CreateProjectFromRepoDto
    {
        public required long GitHubRepoId { get; set; }
        public required string GitHubRepoName { get; set; }
        public string? Title { get; set; }
        public string? Description { get; set; }
        public string? Role { get; set; }
        public string? Image { get; set; }
    }

    // For batch reordering projects
    public class ReorderProjectDto
    {
        public required Guid Id { get; set; }
        public required int DisplayOrder { get; set; }
    }

    // For updating visibility/pin/order of a single project
    public class ProjectVisibilityDto
    {
        public bool IsVisible { get; set; }
        public bool IsPinned { get; set; }
        public int DisplayOrder { get; set; }
    }

    // For hiding/unhiding GitHub repos
    public class HiddenRepoDto
    {
        public required long RepoId { get; set; }
    }
}

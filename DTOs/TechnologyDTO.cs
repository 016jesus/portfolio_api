namespace portfolio_api.DTOs
{
    public class TechnologyDto
    {
        public Guid Id { get; set; }
        public string Title { get; set; } = string.Empty;
        public string Icon { get; set; } = string.Empty;
    }

    public class CreateTechnologyDto
    {
        public required string Title { get; set; }
        public string? Icon { get; set; }
    }

    public class UpdateTechnologyDto
    {
        public required string Title { get; set; }
        public string? Icon { get; set; }
    }
}

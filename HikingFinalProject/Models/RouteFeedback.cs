namespace HikingFinalProject.Models
{
    public class RouteFeedback
    {
        public int Id { get; set; }
        public int RouteID { get; set; }
        public int Strenuousness { get; set; }
        public int Skill { get; set; }
        public int Rating { get; set; }
        public string? Comments { get; set; }
        public string? UserName { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
        public bool IsDeleted { get; set; }

        // Navigation
        public HikingRoute? Route { get; set; }
    }
}

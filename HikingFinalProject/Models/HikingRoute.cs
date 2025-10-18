namespace HikingFinalProject.Models
{
    public class HikingRoute
    {
        public int RouteID { get; set; }
        public string RouteName { get; set; } = string.Empty;

        // Keep ParkID as FK
        public int? ParkID { get; set; }
        public Park? Park { get; set; } = null!; // EF will populate

        public string? Description { get; set; }
        public string? Difficulty { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
        public bool IsDeleted { get; set; }

        public double? Distance { get; set; } // in miles
        public decimal? Latitude { get; set; }
        public decimal? Longitude { get; set; }

        // 🔹 Add navigation collections for popups
        public ICollection<RouteFeedback> Feedback { get; set; } = new List<RouteFeedback>();
        public ICollection<RouteImages> Images { get; set; } = new List<RouteImages>();
    }
}


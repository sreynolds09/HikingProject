using System.Text.Json.Serialization;

namespace HikingFinalProject.DTOs.Routes
{

    public class HikingRouteDto
    {
        public int RouteID { get; set; }
        public string RouteName { get; set; } = string.Empty;
        public int ParkID { get; set; }
        public string? ParkName { get; set; }
        public string? Description { get; set; }
        public string? Difficulty { get; set; }
        public bool IsDeleted { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }

        // Optional rating
        public double? AverageRating { get; set; }

        // For geometry
        public object? GeoJson { get; set; }

        // Marker convenience
        public double? Distance { get; set; }
        public decimal? Latitude { get; set; }
        public decimal? Longitude { get; set; }

        // Related popup data
        public IEnumerable<RouteFeedbackDto>? RecentFeedback { get; set; }
        public IEnumerable<RouteImageDto>? RecentImages { get; set; }
        public List<RoutePointDto> RecentPoints { get; set; } = new List<RoutePointDto>();
        public List<RoutePointDto> Coordinates { get; set; } = new();
        
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public ParkDto? Park { get; set; }
    }

}
// ===== FILE: DTOs/ParkDto.cs =====
using HikingFinalProject.DTOs.Dashboard;
using System.Text.Json.Serialization;


namespace HikingFinalProject.DTOs.Routes
{
    public class ParkDto
    {
        public int ParkID { get; set; }
        public string ParkName { get; set; } = string.Empty;
        public string? Location { get; set; }
        public string? Description { get; set; }
        public bool isDeleted { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
        public decimal? Latitude { get; set; }
        public decimal? Longitude { get; set; }
        public string ImageURL { get; set; } = string.Empty;
        public string Address { get; set; } = string.Empty;

        [JsonIgnore] 
        public List<HikingRouteDto>? Routes { get; set; }
        public List<DashboardImageDto>? RecentImages { get; set; }


    }
}
// ===== FILE: DTOs/RoutePointDto.cs =====
namespace HikingFinalProject.DTOs.Routes
{
    public class RoutePointDto
    {
        public int Id { get; set; }
        public int RouteID { get; set; }
        public decimal? latitude { get; set; }
        public decimal? longitude { get; set; }
        public string? description { get; set; }
        public DateTime? time { get; set; }
        public decimal? elevation { get; set; }
        public int pointOrder { get; set; }
        public bool isDeleted { get; set; }
        public DateTime createdAt { get; set; }
    }
}

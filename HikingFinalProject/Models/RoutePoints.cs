namespace HikingFinalProject.Models
{
    public class RoutePoint
    {
        public int Id { get; set; }
        public int RouteID { get; set; }
        public decimal? Latitude { get; set; }
        public decimal? Longitude { get; set; }
        public string? Description { get; set; }
        public DateTime CreatedAt { get; set; }
        public decimal? Elevation { get; set; }
        public int PointOrder { get; set; }
        public DateTime? Time { get; set; }
        public bool isDeleted { get; set; }

        // Navigation
        public HikingRoute? HikingRoute { get; set; }
    }
}


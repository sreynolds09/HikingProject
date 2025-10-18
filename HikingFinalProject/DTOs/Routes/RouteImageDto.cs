namespace HikingFinalProject.DTOs.Routes
{
    public class RouteImageDto
    {
        public int id { get; set; }
        public int routeId { get; set; }
        public string imageURL { get; set; } = string.Empty;
        public string? caption { get; set; }
        public string fileName { get; set; } = string.Empty;
        public string filePath { get; set; } = string.Empty;
        public DateTime? dateStamp { get; set; }
        public bool isDeleted { get; set; }
        public DateTime createdAt { get; set; }
        public DateTime updatedAt { get; set; }
       public decimal? Latitude { get; set; }
      public decimal? Longitude { get; set; }
    }
}

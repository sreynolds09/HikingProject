namespace HikingFinalProject.Models
{
    public class RouteImages
    {
        public int Id { get; set; }
        public int RouteID { get; set; }
        public string ImageURL { get; set; } = string.Empty;
        public string? Caption { get; set; }
        public DateTime? DateStamp { get; set; }
        public string FileName { get; set; } = string.Empty;
        public byte[]? FileData { get; set; }
        public string FilePath { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
        public bool IsDeleted { get; set; }
        public decimal? Latitude { get; set; }
        public decimal? Longitude { get; set; }

        public HikingRoute? Route { get; set; } // Navigation property
    }
}


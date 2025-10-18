namespace HikingFinalProject.Models
{
    public class Park
    {
        public int ParkID { get; set; }
        public string? ParkName { get; set; }
        public DateTime CreatedAt { get; set; }
        public string? Location { get; set; }
        public string? Description { get; set; }
        public bool IsDeleted { get; set; }
        public DateTime UpdatedAt { get; set; }
        public decimal? Latitude { get; set; }
        public decimal? Longitude { get; set; }

        public string ImageURL { get; set; } = string.Empty;

    }
}

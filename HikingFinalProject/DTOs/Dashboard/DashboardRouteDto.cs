namespace HikingFinalProject.DTOs.Dashboard
{
    public class DashboardRouteDto
    {

        public int RouteID { get; set; }
        public string RouteName { get; set; } = string.Empty;
        public string? ParkName { get; set; }
        public DateTime CreatedAt { get; set; }
        public string? ImageURL { get; set; } // optional
    }
}

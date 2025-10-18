namespace HikingFinalProject.DTOs.Map
{
    public class MarkerRouteDto
    {
        public int RouteID { get; set; }
        public string RouteName { get; set; } = "";
        public string? ImageURL { get; set; } // optional thumbnail
    }
}

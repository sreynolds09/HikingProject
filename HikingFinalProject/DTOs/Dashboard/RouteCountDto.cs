namespace HikingFinalProject.DTOs.Dashboard
{
    public class RouteCountDto
    {

        public string Month { get; set; } = string.Empty; // can also be RouteName for per-route charts
        public int Count { get; set; }
        public int RouteID { get; set; } // needed for chart interactivity
    }
}

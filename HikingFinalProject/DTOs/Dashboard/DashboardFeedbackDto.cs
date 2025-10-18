namespace HikingFinalProject.DTOs.Dashboard
{
    public class DashboardFeedbackDto
    {

        public string UserName { get; set; } = "";
        public int Rating { get; set; }
        public string Comments { get; set; } = "";
        public string RouteName { get; set; } = "";
        public DateTime CreatedAt { get; set; }
        public int RouteID { get; set; }
        public int ParkID { get; set; }
    }
}

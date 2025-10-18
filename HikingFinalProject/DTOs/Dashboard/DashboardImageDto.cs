namespace HikingFinalProject.DTOs.Dashboard
{
    public class DashboardImageDto
    {

        public string ImageURL { get; set; } = "";
        public string Caption { get; set; } = "";
        public int RouteID { get; set; }
        public int ParkID { get; set; }
    }
}
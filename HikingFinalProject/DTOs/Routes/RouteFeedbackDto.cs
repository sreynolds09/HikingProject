namespace HikingFinalProject.DTOs.Routes
{
    public class RouteFeedbackDto
    {
        public int Id { get; set; }
        public int RouteID { get; set; }
        public int Strenuousness { get; set; }
        public string? RouteName { get; set; }
        public int Skill { get; set; }
        public string? Comments { get; set; }
        public int? Rating { get; set; }
        public string? UserName { get; set; }
        public bool isDeleted { get; set; }
        public DateTime CreatedAt { get; set; }
    }
}
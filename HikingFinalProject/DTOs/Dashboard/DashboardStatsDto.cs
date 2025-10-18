using HikingFinalProject.DTOs.Dashboard;

namespace HikingFinalProject.DTOs.Dashboard
{
    public class DashboardStatsDto
    {

        public IEnumerable<int> FeedbackRatings { get; set; } = new List<int>();
        public IEnumerable<RouteCountDto> RoutesOverTime { get; set; } = new List<RouteCountDto>();
        public IEnumerable<RouteCountDto> RoutePointsPerRoute { get; set; } = new List<RouteCountDto>();
        public IEnumerable<RouteCountDto> ImagesPerRoute { get; set; } = new List<RouteCountDto>();
    }
}

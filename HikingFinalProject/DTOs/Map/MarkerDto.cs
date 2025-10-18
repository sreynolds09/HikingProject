using HikingFinalProject.DTOs.Dashboard;
using HikingFinalProject.DTOs.Routes;

namespace HikingFinalProject.DTOs.Map
{
    public class MarkerDto
    {
 
            public double Latitude { get; set; }
            public double Longitude { get; set; }
            public string Label { get; set; } = "";
            public string Name { get; set; } = "";
            public string ParkName { get; set; } = "";
            public int ParkID { get; set; }

            public IEnumerable<DashboardImageDto> Images { get; set; } = new List<DashboardImageDto>();
            public IEnumerable<DashboardFeedbackDto> Feedback { get; set; } = new List<DashboardFeedbackDto>();

            // NEW: route thumbnails for popups
            public IEnumerable<MarkerRouteDto> Routes { get; set; } = new List<MarkerRouteDto>();

    }
}

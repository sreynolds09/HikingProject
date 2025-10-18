using HikingFinalProject.DTOs.Map;
using HikingFinalProject.DTOs.Routes;
using System;
using System.Collections.Generic;

namespace HikingFinalProject.DTOs.Dashboard
{

    public class DashboardDto
    {
        // Core totals
        public int TotalParks { get; set; }
        public int TotalRoutes { get; set; }
        public int TotalFeedback { get; set; }
        public int TotalImages { get; set; }
        public double AverageRouteRating { get; set; }
        public int ParkCount { get; set; }
        public int RouteCount { get; set; }
        public int ImageCount { get; set; }
        public int PointCount { get; set; }
        public List<string> Difficulties { get; set; } = new List<string> { "Easy", "Moderate", "Hard" };


        public IEnumerable<HikingRouteDto> Routes { get; set; } = new List<HikingRouteDto>();
        public IEnumerable<MarkerDto> Markers { get; set; } = new List<MarkerDto>();
        public IEnumerable<ParkDto> Parks { get; set; } = new List<ParkDto>();

        // SPA / site.js properties

        public IEnumerable<DashboardImageDto> RecentImages { get; set; } = new List<DashboardImageDto>();
        public IEnumerable<DashboardFeedbackDto> RecentFeedback { get; set; } = new List<DashboardFeedbackDto>();

        // Optional: Razor charts / dashboards
        public IEnumerable<DashboardRouteDto>? RecentRoutes { get; set; }
        public DashboardStatsDto? Stats { get; set; }
    }

}

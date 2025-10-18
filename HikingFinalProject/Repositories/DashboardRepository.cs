using AutoMapper;
using Dapper;
using HikingFinalProject.DTOs.Dashboard;
using HikingFinalProject.DTOs.Map;
using HikingFinalProject.DTOs.Routes;
using HikingFinalProject.DTOs.Mapbox;
using HikingFinalProject.Repositories.Interfaces;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Threading.Tasks;

namespace HikingFinalProject.Repositories
{
    public class DashboardRepository : IDashboardRepository
    {
        private readonly IDbConnection _conn;
        private readonly IMapper _mapper;

        public DashboardRepository(IDbConnection conn, IMapper mapper)
        {
            _conn = conn;
            _mapper = mapper;
        }

        private string NormalizeImagePath(string path)
        {
            if (string.IsNullOrWhiteSpace(path))
                return "";

            if (path.StartsWith("/uploads") || path.StartsWith("/images"))
                return path;

            var fileName = System.IO.Path.GetFileName(path);
            return "/uploads/" + fileName;
        }

        public async Task<DashboardDto> GetDashboardAsync()
        {
            var dto = new DashboardDto
            {
                TotalParks = await _conn.ExecuteScalarAsync<int>("SELECT COUNT(*) FROM Parks WHERE IsDeleted = 0"),
                TotalRoutes = await _conn.ExecuteScalarAsync<int>("SELECT COUNT(*) FROM Routes WHERE IsDeleted = 0"),
                TotalFeedback = await _conn.ExecuteScalarAsync<int>("SELECT COUNT(*) FROM RouteFeedback WHERE IsDeleted = 0"),
                TotalImages = await _conn.ExecuteScalarAsync<int>("SELECT COUNT(*) FROM RouteImages WHERE IsDeleted = 0"),
                AverageRouteRating = await _conn.ExecuteScalarAsync<double>("SELECT IFNULL(AVG(Rating), 0) FROM RouteFeedback WHERE IsDeleted = 0")
            };

            // -------------------
            // Recent Images
            // -------------------
            var recentImages = await _conn.QueryAsync<DashboardImageDto>(@"
                SELECT FilePath AS ImageURL, '' AS Caption
                FROM RouteImages
                WHERE IsDeleted = 0
                ORDER BY UploadedAt DESC
                LIMIT 5");

            dto.RecentImages = recentImages
                .Select(i => new DashboardImageDto
                {
                    ImageURL = NormalizeImagePath(i.ImageURL),
                    Caption = i.Caption ?? ""
                }).ToList();

            // -------------------
            // Recent Feedback
            // -------------------
            dto.RecentFeedback = (await _conn.QueryAsync<DashboardFeedbackDto>(@"
                SELECT f.UserName, f.Rating, f.Comments, r.RouteName, f.CreatedAt
                FROM RouteFeedback f
                JOIN Routes r ON f.RouteID = r.RouteID
                WHERE f.IsDeleted = 0
                ORDER BY f.CreatedAt DESC
                LIMIT 5")).ToList();

            // -------------------
            // Recent Routes
            // -------------------
            dto.RecentRoutes = (await _conn.QueryAsync<DashboardRouteDto>(@"
                SELECT r.RouteID, r.RouteName, p.ParkName, r.CreatedAt
                FROM Routes r
                LEFT JOIN Parks p ON r.ParkID = p.ParkID
                WHERE r.IsDeleted = 0
                ORDER BY r.CreatedAt DESC
                LIMIT 5")).ToList();

            // -------------------
            // Markers for Map
            // -------------------
            var parks = await _conn.QueryAsync<dynamic>(@"
                SELECT ParkID, ParkName, Latitude, Longitude
                FROM Parks
                WHERE IsDeleted = 0");

            dto.Markers = parks.Select(p => new MarkerDto
            {
                ParkID = p.ParkID,
                ParkName = p.ParkName,
                Name = p.ParkName,
                Label = p.ParkName,
                Latitude = (double)(p.Latitude ?? 0),
                Longitude = (double)(p.Longitude ?? 0),
                Images = dto.RecentImages
                    .Where(img => img.ParkID == p.ParkID)
                    .ToList(),
                Feedback = dto.RecentFeedback
                    .Where(f => f.ParkID == p.ParkID)
                    .ToList(),
                Routes = _mapper.Map<IEnumerable<MarkerRouteDto>>(
                    dto.RecentRoutes.Where(r => r.ParkName == p.ParkName)
                )
            }).ToList();


            return dto;
        }
    }
}

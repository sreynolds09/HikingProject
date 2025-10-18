using Dapper;
using HikingFinalProject.DTOs.Mapbox;
using HikingFinalProject.Models;
using HikingFinalProject.Repositories.Interfaces;
using HikingFinalProject.Services;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Threading.Tasks;

namespace HikingFinalProject.Repositories
{
    public class ParkRepository : IParkRepository
    {
        private readonly IDbConnection _conn;
        private readonly IMapboxGeocodingService _geocoder; // ✅ injected service

        public ParkRepository(IDbConnection conn, IMapboxGeocodingService geocoder)
        {
            _conn = conn;
            _geocoder = geocoder;
        }

        // =========================
        // Basic CRUD
        // =========================
        public async Task<IEnumerable<Park>> GetAllAsync()
        {
            string sql = "SELECT * FROM Parks WHERE IsDeleted = 0 ORDER BY CreatedAt DESC";
            return (await _conn.QueryAsync<Park>(sql)).ToList();
        }

        public async Task<Park?> GetByIdAsync(int parkId)
        {
            string sql = "SELECT * FROM Parks WHERE ParkID=@ParkID AND IsDeleted=0";
            return await _conn.QueryFirstOrDefaultAsync<Park>(sql, new { ParkID = parkId });
        }

        public async Task<int> AddAsync(Park park)
        {
            string sql = @"INSERT INTO Parks (Name, Location, Description, CreatedAt, IsDeleted)
                           VALUES (@Name, @Location, @Description, @CreatedAt, 0);
                           SELECT LAST_INSERT_ID();";
            return await _conn.ExecuteScalarAsync<int>(sql, park);
        }

        public async Task<bool> UpdateAsync(Park park)
        {
            string sql = @"UPDATE Parks 
                           SET Name=@Name, Location=@Location, Description=@Description
                           WHERE ParkID=@ParkID AND IsDeleted=0";
            int rows = await _conn.ExecuteAsync(sql, park);
            return rows > 0;
        }

        public async Task<bool> SoftDeleteAsync(int parkId)
        {
            string sql = "UPDATE Parks SET IsDeleted=1 WHERE ParkID=@ParkID";
            int rows = await _conn.ExecuteAsync(sql, new { ParkID = parkId });
            return rows > 0;
        }

        public async Task<int> CountAsync()
        {
            string sql = "SELECT COUNT(*) FROM Parks WHERE IsDeleted = 0";
            return await _conn.ExecuteScalarAsync<int>(sql);
        }

        // =========================
        // Coordinates
        // =========================
        public async Task<bool> UpdateCoordinatesAsync(int parkId, double latitude, double longitude)
        {
            string sql = "UPDATE Parks SET Latitude=@Latitude, Longitude=@Longitude WHERE ParkID=@ParkID AND IsDeleted=0";
            int rows = await _conn.ExecuteAsync(sql, new { Latitude = latitude, Longitude = longitude, ParkID = parkId });
            return rows > 0;
        }

        public async Task<int> GeocodeMissingParksAsync()
        {
            string sqlSelect = "SELECT * FROM Parks WHERE (Latitude IS NULL OR Longitude IS NULL) AND IsDeleted = 0";
            var parks = (await _conn.QueryAsync<Park>(sqlSelect)).ToList();
            if (!parks.Any()) return 0;

            int updatedCount = 0;

            foreach (var park in parks)
            {
                string query = park.ParkName ?? $"Park {park.ParkID}";

                try
                {
                    var coords = await _geocoder.GeocodeAsync(query);

                    if (coords.HasValue)
                    {
                        double lat = coords.Value.lat;
                        double lng = coords.Value.lng;

                        string sqlUpdate = @"UPDATE Parks 
                                             SET Latitude = @lat, Longitude = @lng, UpdatedAt = NOW() 
                                             WHERE ParkID = @id";
                        int rows = await _conn.ExecuteAsync(sqlUpdate, new { lat, lng, id = park.ParkID });

                        if (rows > 0)
                            updatedCount++;
                    }
                }
                catch
                {
                    // Skip failures
                }
            }

            return updatedCount;
        }
    }
}





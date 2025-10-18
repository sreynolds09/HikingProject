using Dapper;
using HikingFinalProject.Models;
using HikingFinalProject.Repositories.Interfaces;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Threading.Tasks;

namespace HikingFinalProject.Repositories
{
    public class RouteFeedbackRepository : IRouteFeedbackRepository
    {
        private readonly IDbConnection _conn;

        public RouteFeedbackRepository(IDbConnection conn)
        {
            _conn = conn;
        }

        public async Task<IEnumerable<RouteFeedback>> GetAllAsync()
        {
            string sql = "SELECT * FROM routefeedback WHERE IsDeleted = 0 ORDER BY CreatedAt DESC";
            return (await _conn.QueryAsync<RouteFeedback>(sql)).ToList();
        }

        public async Task<RouteFeedback?> GetByIdAsync(int id)
        {
            string sql = "SELECT * FROM routefeedback WHERE Id = @Id AND IsDeleted = 0";
            return await _conn.QueryFirstOrDefaultAsync<RouteFeedback>(sql, new { Id = id });
        }

        public async Task<IEnumerable<RouteFeedback>> GetByRouteIdAsync(int routeId)
        {
            string sql = "SELECT * FROM routefeedback WHERE RouteID = @RouteID AND IsDeleted = 0 ORDER BY CreatedAt DESC";
            return (await _conn.QueryAsync<RouteFeedback>(sql, new { RouteID = routeId })).ToList();
        }

        public async Task<int> CreateAsync(RouteFeedback feedback)
        {
            string sql = @"
                INSERT INTO routefeedback(RouteID, Strenuousness, Skill, Comments, Rating, UserName, CreatedAt, IsDeleted)
                VALUES(@RouteID, @Strenuousness, @Skill, @Comments, @Rating, @UserName, @CreatedAt, 0);
                SELECT LAST_INSERT_ID();";
            return await _conn.ExecuteScalarAsync<int>(sql, feedback);
        }

        public async Task<bool> UpdateAsync(RouteFeedback feedback)
        {
            string sql = @"
                UPDATE routefeedback 
                SET Strenuousness=@Strenuousness, Skill=@Skill, Comments=@Comments, Rating=@Rating, UpdatedAt=@UpdatedAt
                WHERE Id=@Id AND IsDeleted=0";
            int rows = await _conn.ExecuteAsync(sql, feedback);
            return rows > 0;
        }

        public async Task<bool> SoftDeleteAsync(int id)
        {
            string sql = "UPDATE routefeedback SET IsDeleted=1 WHERE Id=@Id";
            int rows = await _conn.ExecuteAsync(sql, new { Id = id });
            return rows > 0;
        }

        public async Task<int> CountAsync()
        {
            string sql = "SELECT COUNT(*) FROM routefeedback WHERE IsDeleted=0";
            return await _conn.ExecuteScalarAsync<int>(sql);
        }

        public async Task<IEnumerable<RouteFeedback>> GetRecentAsync(int count)
        {
            string sql = "SELECT * FROM routefeedback WHERE IsDeleted=0 ORDER BY CreatedAt DESC LIMIT @Count";
            return (await _conn.QueryAsync<RouteFeedback>(sql, new { Count = count })).ToList();
        }

        public async Task<(double avgRating, double avgSkill, double avgStrenuousness)?> GetAggregatesAsync(int routeId)
        {
            string sql = @"
                SELECT AVG(Rating) as AvgRating,
                       AVG(Skill) as AvgSkill,
                       AVG(Strenuousness) as AvgStrenuousness
                FROM routefeedback
                WHERE RouteID = @RouteID AND IsDeleted = 0";

            var result = await _conn.QueryFirstOrDefaultAsync(sql, new { RouteID = routeId });

            if (result == null) return null;

            return (
                avgRating: result.AvgRating ?? 0,
                avgSkill: result.AvgSkill ?? 0,
                avgStrenuousness: result.AvgStrenuousness ?? 0
            );
        }
    }
}

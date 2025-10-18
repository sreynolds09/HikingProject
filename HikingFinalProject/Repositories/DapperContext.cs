using System.Data;
using MySql.Data.MySqlClient;
using Microsoft.Extensions.Configuration;
using HikingFinalProject.Repositories.Interfaces;

namespace HikingFinalProject.Repositories
{
    public class DapperContext : IDapperContext
    {
        private readonly string _connectionString;

        public DapperContext(IConfiguration configuration)
        {
            _connectionString = configuration.GetConnectionString("DefaultConnection")
                                ?? throw new InvalidOperationException("Connection string 'DefaultConnection' missing.");
        }

        public IDbConnection CreateConnection()
        {
            var connection = new MySqlConnection(_connectionString);
            connection.Open();
            return connection;
        }
    }
}

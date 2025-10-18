using System.Data;

namespace HikingFinalProject.Repositories.Interfaces;

public interface IDapperContext
{
    IDbConnection CreateConnection();
}


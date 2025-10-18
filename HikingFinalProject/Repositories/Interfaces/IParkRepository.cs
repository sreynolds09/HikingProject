using HikingFinalProject.Models;

namespace HikingFinalProject.Repositories.Interfaces
{
    public interface IParkRepository
    {
        Task<IEnumerable<Park>> GetAllAsync();
        Task<Park?> GetByIdAsync(int id);
        Task<int> AddAsync(Park park);
        Task<bool> UpdateAsync(Park park);
        Task<bool> SoftDeleteAsync(int id);
        Task<int> CountAsync();

        // Coordinates
        Task<bool> UpdateCoordinatesAsync(int parkId, double latitude, double longitude);
        Task<int> GeocodeMissingParksAsync(); // ✅ no apiKey here
    }
}

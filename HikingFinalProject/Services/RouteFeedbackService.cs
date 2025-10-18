using AutoMapper;
using HikingFinalProject.DTOs.Routes;
using HikingFinalProject.Models;
using HikingFinalProject.Repositories.Interfaces;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace HikingFinalProject.Services
{
    public interface IRouteFeedbackService
    {
        Task<IEnumerable<RouteFeedbackDto>> GetAllAsync();
        Task<RouteFeedbackDto?> GetByIdAsync(int id);
        Task<IEnumerable<RouteFeedbackDto>> GetByRouteIdAsync(int routeId);
        Task<int> CreateAsync(RouteFeedbackDto dto);
        Task<bool> UpdateAsync(RouteFeedbackDto dto);
        Task<bool> SoftDeleteAsync(int id);
        Task<int> CountAsync();
        Task<IEnumerable<RouteFeedbackDto>> GetRecentAsync(int count);
        Task<(double avgRating, double avgSkill, double avgStrenuousness)?> GetAggregatesAsync(int routeId);

        IMapper Mapper { get; }
    }

    public class RouteFeedbackService : IRouteFeedbackService
    {
        private readonly IRouteFeedbackRepository _repo;
        private readonly IMapper _mapper;

        public RouteFeedbackService(IRouteFeedbackRepository repo, IMapper mapper)
        {
            _repo = repo;
            _mapper = mapper;
        }

        public IMapper Mapper => _mapper;

        public async Task<IEnumerable<RouteFeedbackDto>> GetAllAsync()
        {
            var list = await _repo.GetAllAsync();
            return _mapper.Map<IEnumerable<RouteFeedbackDto>>(list);
        }

        public async Task<RouteFeedbackDto?> GetByIdAsync(int id)
        {
            var feedback = await _repo.GetByIdAsync(id);
            return _mapper.Map<RouteFeedbackDto>(feedback);
        }

        public async Task<IEnumerable<RouteFeedbackDto>> GetByRouteIdAsync(int routeId)
        {
            var list = await _repo.GetByRouteIdAsync(routeId);
            return _mapper.Map<IEnumerable<RouteFeedbackDto>>(list);
        }

        public async Task<int> CreateAsync(RouteFeedbackDto dto)
        {
            var feedback = _mapper.Map<RouteFeedback>(dto);
            return await _repo.CreateAsync(feedback);
        }

        public async Task<bool> UpdateAsync(RouteFeedbackDto dto)
        {
            var feedback = _mapper.Map<RouteFeedback>(dto);
            return await _repo.UpdateAsync(feedback);
        }

        public async Task<bool> SoftDeleteAsync(int id) => await _repo.SoftDeleteAsync(id);

        public async Task<int> CountAsync() => await _repo.CountAsync();

        public async Task<IEnumerable<RouteFeedbackDto>> GetRecentAsync(int count)
        {
            var list = await _repo.GetRecentAsync(count);
            return _mapper.Map<IEnumerable<RouteFeedbackDto>>(list);
        }

        public async Task<(double avgRating, double avgSkill, double avgStrenuousness)?> GetAggregatesAsync(int routeId)
        {
            return await _repo.GetAggregatesAsync(routeId);
        }
    }
}

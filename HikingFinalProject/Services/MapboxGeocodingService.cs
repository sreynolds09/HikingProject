using HikingFinalProject.DTOs.Mapbox;
using Microsoft.Extensions.Options;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using System;
using System.Linq;

namespace HikingFinalProject.Services
{
    public interface IMapboxGeocodingService
    {
        Task<(double lat, double lng)?> GeocodeAsync(string query);
    }

    public class MapboxOptions
    {
        public string AccessToken { get; set; } = string.Empty;
    }

    public class MapboxGeocodingService : IMapboxGeocodingService
    {
        private readonly HttpClient _httpClient;
        private readonly string _token;
        private readonly ILogger<MapboxGeocodingService> _logger;

        public MapboxGeocodingService(HttpClient httpClient, IOptions<MapboxOptions> options, ILogger<MapboxGeocodingService> logger)
        {
            _httpClient = httpClient;
            _logger = logger;
            _token = options.Value.AccessToken ?? string.Empty;
        }

        public async Task<(double lat, double lng)?> GeocodeAsync(string query)
        {
            if (string.IsNullOrWhiteSpace(query))
            {
                _logger.LogWarning("GeocodeAsync called with empty query.");
                return null;
            }

            if (string.IsNullOrWhiteSpace(_token))
            {
                _logger.LogWarning("Mapbox AccessToken is not configured. Cannot geocode '{Query}'", query);
                return null;
            }

            try
            {
                var url = $"https://api.mapbox.com/geocoding/v5/mapbox.places/{Uri.EscapeDataString(query)}.json?access_token={_token}&limit=1";
                var response = await _httpClient.GetFromJsonAsync<MapboxResponseDto>(url);

                var feature = response?.Features?.FirstOrDefault();
                if (feature == null)
                {
                    _logger.LogInformation("No Mapbox feature found for query '{Query}'", query);
                    return null;
                }

                // Prefer "center", fallback to geometry.coordinates
                double[]? coords = feature.Center ?? feature.Geometry?.Coordinates;
                if (coords?.Length == 2)
                {
                    return (coords[1], coords[0]); // Mapbox = [lng, lat]
                }

                _logger.LogInformation("Mapbox returned invalid coordinates for query '{Query}'", query);
                return null;
            }
            catch (HttpRequestException httpEx)
            {
                _logger.LogError(httpEx, "HTTP error while geocoding '{Query}'", query);
                return null;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error while geocoding '{Query}'", query);
                return null;
            }
        }
    }
}

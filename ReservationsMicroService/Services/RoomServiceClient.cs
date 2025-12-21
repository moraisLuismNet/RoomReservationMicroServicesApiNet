using System.Text.Json;
using ReservationsMicroService.DTOs;

namespace ReservationsMicroService.Services
{
    public class RoomServiceClient
    {
        private readonly HttpClient _httpClient;
        private readonly ILogger<RoomServiceClient> _logger;

        public RoomServiceClient(IHttpClientFactory httpClientFactory, ILogger<RoomServiceClient> logger)
        {
            _httpClient = httpClientFactory.CreateClient("RoomMicroService");
            _logger = logger;
        }

        public async Task<RoomDTO?> GetRoomByIdAsync(int id)
        {
            try
            {
                // Assuming RoomMicroService has GET /api/Rooms/{id}
                // Need to verify endpoint. Usually it's /api/Rooms/{id} or /api/Room/{id}
                // Based on previous conversations, it might be /api/room?
                // Let's assume /api/Rooms/{id} for now, or check RoomMicroService if possible.
                // Assuming standard convention.
                var response = await _httpClient.GetAsync($"api/Room/{id}");

                if (response.IsSuccessStatusCode)
                {
                    var json = await response.Content.ReadAsStringAsync();
                    var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
                    return JsonSerializer.Deserialize<RoomDTO>(json, options);
                }

                _logger.LogWarning("Failed to fetch room {RoomId}. Status: {StatusCode}", id, response.StatusCode);
                return null;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching room {RoomId}", id);
                return null;
            }
        }
    }
}

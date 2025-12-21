using PaymentMicroService.Models;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;

namespace PaymentMicroService.Services
{
    public class ReservationServiceClient
    {
        private readonly HttpClient _httpClient;
        private readonly ILogger<ReservationServiceClient> _logger;

        public ReservationServiceClient(IHttpClientFactory httpClientFactory, ILogger<ReservationServiceClient> logger)
        {
            _httpClient = httpClientFactory.CreateClient("ReservationsMicroService");
            _logger = logger;
        }

        public async Task<Reservation?> GetReservationByIdAsync(int id)
        {
            try
            {
                _logger.LogInformation("Getting reservation by id {Id}", id);
                var response = await _httpClient.GetAsync($"api/Reservations/{id}");

                if (response.IsSuccessStatusCode)
                {
                    var content = await response.Content.ReadAsStringAsync();
                    var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
                    return JsonSerializer.Deserialize<Reservation>(content, options);
                }
                
                if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
                {
                    return null;
                }

                _logger.LogWarning("Failed to get reservation by id {Id}. Status Code: {StatusCode}", id, response.StatusCode);
                return null;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting reservation by id {Id}", id);
                return null;
            }
        }

        public async Task<bool> ConfirmReservationAsync(int id)
        {
            try
            {
                _logger.LogInformation("Confirming reservation {Id} at ReservationsMicroService", id);
                // Note: The endpoint is POST api/Reservations/{id}/confirm
                var response = await _httpClient.PostAsync($"api/Reservations/{id}/confirm", null);

                if (response.IsSuccessStatusCode)
                {
                    _logger.LogInformation("Successfully confirmed reservation {Id}", id);
                    return true;
                }

                _logger.LogWarning("Failed to confirm reservation {Id}. Status Code: {StatusCode}", id, response.StatusCode);
                return false;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error confirming reservation {Id}", id);
                return false;
            }
        }
    }
}

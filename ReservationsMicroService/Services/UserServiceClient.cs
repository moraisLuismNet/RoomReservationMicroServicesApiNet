using System.Text.Json;
using ReservationsMicroService.DTOs;

namespace ReservationsMicroService.Services
{
    public class UserServiceClient
    {
        private readonly HttpClient _httpClient;
        private readonly ILogger<UserServiceClient> _logger;
        private readonly IHttpContextAccessor _httpContextAccessor;

        public UserServiceClient(IHttpClientFactory httpClientFactory, ILogger<UserServiceClient> logger, IHttpContextAccessor httpContextAccessor)
        {
            _httpClient = httpClientFactory.CreateClient("UserAndAuthorizationManagementMicroService");
            _logger = logger;
            _httpContextAccessor = httpContextAccessor;
        }

        public async Task<UserDTO?> GetUserByEmailAsync(string email)
        {
            try
            {
                // Forward the Authorization header from the current request
                var authHeader = _httpContextAccessor.HttpContext?.Request.Headers["Authorization"].FirstOrDefault();
                if (!string.IsNullOrEmpty(authHeader))
                {
                    _httpClient.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", authHeader.Replace("Bearer ", ""));
                }

                var response = await _httpClient.GetAsync($"api/Users/{email}");

                if (response.IsSuccessStatusCode)
                {
                    var funcJson = await response.Content.ReadAsStringAsync();
                    var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
                    return JsonSerializer.Deserialize<UserDTO>(funcJson, options);
                }
                
                _logger.LogWarning("Failed to fetch user {Email}. Status: {StatusCode}", email, response.StatusCode);
                return null;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching user {Email}", email);
                return null;
            }
        }
    }
}

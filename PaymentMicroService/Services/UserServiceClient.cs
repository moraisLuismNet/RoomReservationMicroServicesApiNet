using PaymentMicroService.DTOs;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;

namespace PaymentMicroService.Services
{
    public class UserServiceClient
    {
        private readonly HttpClient _httpClient;
        private readonly ILogger<UserServiceClient> _logger;

        public UserServiceClient(IHttpClientFactory httpClientFactory, ILogger<UserServiceClient> logger)
        {
            _httpClient = httpClientFactory.CreateClient("UserAndAuthorizationManagementMicroService");
            _logger = logger;
        }

        public async Task<UserDTO?> GetUserByEmailAsync(string email)
        {
            try
            {
                var response = await _httpClient.GetAsync($"api/Users/{email}");

                if (response.IsSuccessStatusCode)
                {
                    var content = await response.Content.ReadAsStringAsync();
                    var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
                    return JsonSerializer.Deserialize<UserDTO>(content, options);
                }
                
                if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
                {
                    return null;
                }

                _logger.LogWarning("Failed to get user by email {Email}. Status Code: {StatusCode}", email, response.StatusCode);
                return null;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting user by email {Email}", email);
                return null;
            }
        }
    }
}

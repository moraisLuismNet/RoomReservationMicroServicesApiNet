using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;

namespace PaymentMicroService.Services
{
    public class EmailServiceClient 
    {
        private readonly HttpClient _httpClient;
        private readonly ILogger<EmailServiceClient> _logger;

        public EmailServiceClient(IHttpClientFactory httpClientFactory, ILogger<EmailServiceClient> logger)
        {
            _httpClient = httpClientFactory.CreateClient("SendingEmailsMicroService");
            _logger = logger;
        }

        public async Task SendEmailAsync(string toEmail, string subject, string body, string type, int entityId)
        {
            try
            {
                var emailDto = new 
                {
                    ToEmail = toEmail,
                    Subject = subject,
                    Body = body,
                    EmailType = type, // Changed from Type to EmailType
                    ReservationId = entityId, // Changed from EntityId to ReservationId (assuming entityId is reservationId)
                    ScheduledSendTime = DateTime.UtcNow, // Required field
                    Status = "pending",
                    Attempts = 0,
                    MaxAttempts = 3,
                    CreatedAt = DateTime.UtcNow
                };

                var json = JsonSerializer.Serialize(emailDto);
                _logger.LogInformation("Sending POST to SendingEmailsMicroService: {Url}. Payload: {Payload}", "api/EmailQueue", json);
                var content = new StringContent(json, System.Text.Encoding.UTF8, "application/json");

                var response = await _httpClient.PostAsync("api/EmailQueue", content);

                if (response.IsSuccessStatusCode)
                {
                    _logger.LogInformation("Successfully queued email to {ToEmail}", toEmail);
                }
                else
                {
                    var responseBody = await response.Content.ReadAsStringAsync();
                    _logger.LogWarning("Failed to queue email to {ToEmail}. Status Code: {StatusCode}. Response: {Response}", toEmail, response.StatusCode, responseBody);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error queuing email to {ToEmail}", toEmail);
            }
        }
    }
}

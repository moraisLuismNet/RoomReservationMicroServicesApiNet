using System.Text;
using System.Text.Json;

namespace ReservationsMicroService.Services
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
                    EmailType = type,
                    ReservationId = entityId,
                    ScheduledSendTime = DateTime.UtcNow,
                    Status = "pending",
                    Attempts = 0,
                    MaxAttempts = 3,
                    CreatedAt = DateTime.UtcNow
                };

                var json = JsonSerializer.Serialize(emailDto);
                var content = new StringContent(json, Encoding.UTF8, "application/json");

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

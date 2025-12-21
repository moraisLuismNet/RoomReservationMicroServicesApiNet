using System.Text;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using SendingEmailsMicroService.Models;
using SendingEmailsMicroService.Data;

namespace SendingEmailsMicroService.Services
{
    public class EmailService
    {
        private readonly SendingEmailsDbContext _context;
        private readonly EmailConfiguration _emailConfig;
        private readonly HttpClient _httpClient;
        private readonly ILogger<EmailService> _logger;

        public EmailService(SendingEmailsDbContext context, EmailConfiguration emailConfig, HttpClient httpClient, ILogger<EmailService> logger)
        {
            _context = context;
            _emailConfig = emailConfig;
            _httpClient = httpClient;
            _logger = logger;
        }

        public async Task SendEmailAsync(string toEmail, string subject, string body, string emailType, int? reservationId = null)
        {
            var emailQueue = new EmailQueue
            {
                ToEmail = toEmail,
                Subject = subject,
                Body = body,
                EmailType = emailType,
                Status = "pending",
                ScheduledSendTime = DateTime.UtcNow,
                ReservationId = reservationId,
                CreatedAt = DateTime.UtcNow,
                ErrorMessage = string.Empty,
                Metadata = string.Empty,

            };

            _context.EmailQueues.Add(emailQueue);
            await _context.SaveChangesAsync();

            // Process the email queue
            await ProcessEmailQueue();
        }

        public async Task ProcessEmailQueue()
        {
            _logger.LogInformation("Processing email queue. Looking for pending/retrying emails...");
            var pendingEmails = await _context.EmailQueues
                .Where(e => (e.Status == "pending" || e.Status == "retrying") && e.ScheduledSendTime <= DateTime.UtcNow)
                .ToListAsync();

            foreach (var email in pendingEmails)
            {
                try
                {
                    await SendEmail(email);
                    email.Status = "sent";
                    email.SentAt = DateTime.UtcNow;
                }
                catch (Exception ex)
                {
                    email.Status = "failed";
                    email.ErrorMessage = ex.Message;
                    email.Attempts++;

                    if (email.Attempts < email.MaxAttempts)
                    {
                        email.Status = "retrying";
                        email.ScheduledSendTime = DateTime.UtcNow.AddMinutes(5);
                    }
                }
            }

            await _context.SaveChangesAsync();
        }

        private async Task SendEmail(EmailQueue email)
        {
            var emailRequest = new
            {
                sender = new { email = _emailConfig.FromEmail, name = _emailConfig.FromName },
                to = new[] { new { email = email.ToEmail } },
                subject = email.Subject,
                htmlContent = email.Body
            };

            var json = JsonSerializer.Serialize(emailRequest);
            _logger.LogInformation("Sending email via Brevo API. To: {ToEmail}, Subject: {Subject}", email.ToEmail, email.Subject);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            using var message = new HttpRequestMessage(HttpMethod.Post, "https://api.brevo.com/v3/smtp/email");
            message.Headers.Add("api-key", _emailConfig.BrevoApiKey);
            message.Headers.Add("accept", "application/json");
            message.Content = content;

            var response = await _httpClient.SendAsync(message);

            if (!response.IsSuccessStatusCode)
            {
                var responseContent = await response.Content.ReadAsStringAsync();
                _logger.LogError("Failed to send email via Brevo. Status: {StatusCode}, Response: {Response}", response.StatusCode, responseContent);
                throw new Exception($"Failed to send email: {response.StatusCode} - {responseContent}");
            }

            _logger.LogInformation("Successfully sent email to {ToEmail}", email.ToEmail);
        }
    }

    public class EmailConfiguration
    {
        public required string FromEmail { get; set; }
        public required string FromName { get; set; }
        public required string BrevoApiKey { get; set; }
    }
}

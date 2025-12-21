using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SendingEmailsMicroService.Data;
using SendingEmailsMicroService.DTOs;
using SendingEmailsMicroService.Helpers;
using SendingEmailsMicroService.Models;

namespace SendingEmailsMicroService.Services
{
  public class EmailQueueService : IEmailQueueService
  {
    private readonly SendingEmailsDbContext _context;
    private readonly ILogger<EmailQueueService> _logger;

    public EmailQueueService(SendingEmailsDbContext context, ILogger<EmailQueueService> logger)
    {
      _context = context;
      _logger = logger;
    }

    public async Task<ActionResult<IEnumerable<EmailQueueDTO>>> GetAllEmailQueues()
    {
      var emailQueues = await _context.EmailQueues
          .Select(e => new EmailQueueDTO
          {
            EmailQueueId = e.EmailQueueId,
            ToEmail = e.ToEmail,
            Subject = e.Subject,
            Body = e.Body,
            EmailType = e.EmailType,
            Status = e.Status,
            Attempts = e.Attempts,
            MaxAttempts = e.MaxAttempts,
            ScheduledSendTime = e.ScheduledSendTime,
            SentAt = e.SentAt,
            ErrorMessage = e.ErrorMessage,
            CreatedAt = e.CreatedAt,
            ReservationId = e.ReservationId,
            Metadata = e.Metadata
          })
          .ToListAsync();

      return emailQueues;
    }

    public async Task<ActionResult<EmailQueueDTO>> GetEmailQueue(int id)
    {
      var emailQueue = await _context.EmailQueues.FindAsync(id);

      if (emailQueue == null)
      {
        return new NotFoundResult();
      }

      var emailQueueDto = EmailQueueHelpers.ConvertToEmailQueueDTO(emailQueue);

      return emailQueueDto;
    }

    public async Task<IActionResult> PutEmailQueue(int id, UpdateEmailQueueDTO updateEmailQueueDto)
    {
      if (id != updateEmailQueueDto.EmailQueueId)
      {
        return new BadRequestResult();
      }

      var emailQueue = await _context.EmailQueues.FindAsync(id);

      if (emailQueue == null)
      {
        return new NotFoundResult();
      }

      emailQueue.ToEmail = updateEmailQueueDto.ToEmail ?? string.Empty;
      emailQueue.Subject = updateEmailQueueDto.Subject ?? string.Empty;
      emailQueue.Body = updateEmailQueueDto.Body ?? string.Empty;
      emailQueue.EmailType = updateEmailQueueDto.EmailType ?? string.Empty;
      emailQueue.Status = updateEmailQueueDto.Status ?? string.Empty;
      emailQueue.Attempts = updateEmailQueueDto.Attempts;
      emailQueue.MaxAttempts = updateEmailQueueDto.MaxAttempts;
      emailQueue.ScheduledSendTime = updateEmailQueueDto.ScheduledSendTime;
      emailQueue.SentAt = updateEmailQueueDto.SentAt;
      emailQueue.ErrorMessage = updateEmailQueueDto.ErrorMessage ?? string.Empty;
      emailQueue.CreatedAt = updateEmailQueueDto.CreatedAt;
      emailQueue.ReservationId = updateEmailQueueDto.ReservationId;
      emailQueue.Metadata = updateEmailQueueDto.Metadata ?? string.Empty;

      _context.Entry(emailQueue).State = EntityState.Modified;

      try
      {
        await _context.SaveChangesAsync();
      }
      catch (DbUpdateConcurrencyException)
      {
        if (!EmailQueueHelpers.EmailQueueExists(id, _context))
        {
          return new NotFoundResult();
        }
        else
        {
          throw;
        }
      }

      return new NoContentResult();
    }

    public async Task<ActionResult<EmailQueueDTO>> PostEmailQueue(CreateEmailQueueDTO createEmailQueueDto)
    {
      _logger.LogInformation("Queuing new email for {ToEmail} (Type: {EmailType})", createEmailQueueDto.ToEmail, createEmailQueueDto.EmailType);
      var emailQueue = new EmailQueue
      {
        ToEmail = createEmailQueueDto.ToEmail ?? string.Empty,
        Subject = createEmailQueueDto.Subject ?? string.Empty,
        Body = createEmailQueueDto.Body ?? string.Empty,
        EmailType = createEmailQueueDto.EmailType ?? string.Empty,
        Status = createEmailQueueDto.Status ?? string.Empty,
        Attempts = createEmailQueueDto.Attempts,
        MaxAttempts = createEmailQueueDto.MaxAttempts,
        ScheduledSendTime = createEmailQueueDto.ScheduledSendTime,
        SentAt = createEmailQueueDto.SentAt,
        ErrorMessage = createEmailQueueDto.ErrorMessage ?? string.Empty,
        CreatedAt = createEmailQueueDto.CreatedAt,
        ReservationId = createEmailQueueDto.ReservationId,
        Metadata = createEmailQueueDto.Metadata ?? string.Empty
      };

      _context.EmailQueues.Add(emailQueue);
      await _context.SaveChangesAsync();

      var emailQueueDto = EmailQueueHelpers.ConvertToEmailQueueDTO(emailQueue);

      return emailQueueDto;
    }

    public async Task<IActionResult> DeleteEmailQueue(int id)
    {
      var emailQueue = await _context.EmailQueues.FindAsync(id);

      if (emailQueue == null)
      {
        return new NotFoundResult();
      }

      _context.EmailQueues.Remove(emailQueue);
      await _context.SaveChangesAsync();

      return new NoContentResult();
    }
  }
}

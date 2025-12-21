using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SendingEmailsMicroService.DTOs;
using SendingEmailsMicroService.Services;

namespace SendingEmailsMicroService.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize(Roles = "admin")]
    public class EmailQueueController : ControllerBase
    {
        private readonly IEmailQueueService _emailQueueService;

        public EmailQueueController(IEmailQueueService emailQueueService)
        {
            _emailQueueService = emailQueueService;
        }

        // GET: api/EmailQueue
        [HttpGet]
        public async Task<ActionResult<IEnumerable<EmailQueueDTO>>> GetEmailQueues()
        {
            var result = await _emailQueueService.GetAllEmailQueues();
            return Ok(result.Value);
        }

        // GET: api/EmailQueue/5
        [HttpGet("{id}")]
        public async Task<ActionResult<EmailQueueDTO>> GetEmailQueue(int id)
        {
            var emailQueue = await _emailQueueService.GetEmailQueue(id);
            if (emailQueue.Result != null && (emailQueue.Result is NotFoundResult))
            {
                return NotFound();
            }

            return emailQueue;
        }

        // PUT: api/EmailQueue/5
        [HttpPut("{id}")]
        public async Task<IActionResult> PutEmailQueue(int id, UpdateEmailQueueDTO updateEmailQueueDto)
        {
            return await _emailQueueService.PutEmailQueue(id, updateEmailQueueDto);
        }

        // POST: api/EmailQueue
        [HttpPost]
        [AllowAnonymous]
        public async Task<ActionResult<EmailQueueDTO>> PostEmailQueue(CreateEmailQueueDTO createEmailQueueDto)
        {
            return await _emailQueueService.PostEmailQueue(createEmailQueueDto);
        }

        // DELETE: api/EmailQueue/5
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteEmailQueue(int id)
        {
            return await _emailQueueService.DeleteEmailQueue(id);
        }
    }
}

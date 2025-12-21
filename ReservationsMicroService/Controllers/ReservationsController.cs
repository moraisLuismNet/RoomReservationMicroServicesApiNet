using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ReservationsMicroService.DTOs;
using ReservationsMicroService.Models;
using ReservationsMicroService.Services;

namespace ReservationsMicroService.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class ReservationsController : ControllerBase
    {
        private readonly IReservationService _reservationService;
        private readonly ILogger<ReservationsController> _logger;

        public ReservationsController(IReservationService reservationService, ILogger<ReservationsController> logger)
        {
            _reservationService = reservationService;
            _logger = logger;
        }

        // GET: api/Reservations
        [HttpGet]
        public async Task<ActionResult<IEnumerable<ReservationDTO>>> GetReservations()
        {
            if (!User.IsInRole("admin"))
            {
                return Forbid();
            }

            return Ok(await _reservationService.GetAllReservations());
        }

        // GET: api/Reservations/by-email/email
        [HttpGet("by-email/{email}")]
        public async Task<ActionResult<IEnumerable<ReservationDTO>>> GetReservation(string email)
        {
            if (!User.IsInRole("admin") && (User.Identity == null || User.Identity.Name != email))
            {
                return Forbid();
            }

            _logger.LogInformation("Fetching reservations for email: {Email}", email);
            var reservations = await _reservationService.GetReservationsByEmail(email);
            _logger.LogInformation("Fetched {Count} reservations for email {Email}", reservations?.Count() ?? 0, email);
            if (reservations == null || !reservations.Any())
            {
                _logger.LogWarning("No reservations found for email: {Email}", email);
                return NotFound();
            }

            return Ok(reservations);
        }

        // GET: api/Reservations/room/5
        [HttpGet("room/{roomId}")]
        [AllowAnonymous] // Allow viewing availability without login? Or maybe Authorize? User request says "When booking", so user is likely logged in. But safer to AllowAnonymous if we want public availability. I'll stick to Authorize for now as per controller attribute, or override. Let's make it public for now so anyone can see availability.
        public async Task<ActionResult<IEnumerable<ReservationDTO>>> GetReservationsByRoom(int roomId)
        {
            var reservations = await _reservationService.GetReservationsByRoomId(roomId);
            return Ok(reservations);
        }

        // GET: api/Reservations/5
        [HttpGet("{id:int}")]
        [AllowAnonymous]
        public async Task<ActionResult<ReservationDTO>> GetReservation(int id)
        {
            _logger.LogInformation("Fetching reservation by ID: {Id}", id);
            var reservation = await _reservationService.GetReservation(id);

            if (reservation == null)
            {
                _logger.LogWarning("Reservation with ID {Id} not found", id);
                return NotFound();
            }

            return Ok(reservation);
        }

        // PUT: api/Reservations/5
        [HttpPut("{id}")]
        public async Task<IActionResult> PutReservation(int id, Reservation reservation)
        {
            return await _reservationService.PutReservation(id, reservation);
        }

        // POST: api/Reservations
        [HttpPost]
        public async Task<ActionResult<ReservationDTO>> PostReservation(CreateReservationDTO createReservationDto)
        {
            return await _reservationService.PostReservation(createReservationDto);
        }

        // DELETE: api/Reservations/5
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteReservation(int id)
        {
            return await _reservationService.DeleteReservation(id);
        }
        [HttpPost("{id}/confirm")]
        [AllowAnonymous] // Allow internal service call without user context
        public async Task<IActionResult> ConfirmReservation(int id)
        {
            _logger.LogInformation("Confirming reservation with ID: {Id}", id);
            return await _reservationService.ConfirmReservation(id);
        }
    }
}

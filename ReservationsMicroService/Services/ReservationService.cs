using Microsoft.AspNetCore.Mvc;
using ReservationsMicroService.DTOs;
using ReservationsMicroService.Helpers;
using ReservationsMicroService.Models;
using ReservationsMicroService.Repository;
using System.Text.Json;


namespace ReservationsMicroService.Services
{
    public class ReservationService : IReservationService
    {
        private readonly IReservationRepository _reservationRepository;
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly ILogger<ReservationService> _logger;
        private readonly IReservationStatusRepository _reservationStatusRepository;
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly UserServiceClient _userServiceClient;
        private readonly EmailServiceClient _emailServiceClient;
        private readonly RoomServiceClient _roomServiceClient;
        private readonly IConfiguration _configuration;

        public ReservationService(
            IReservationRepository reservationRepository,
            IHttpContextAccessor httpContextAccessor,
            ILogger<ReservationService> logger,
            IReservationStatusRepository reservationStatusRepository,
            IHttpClientFactory httpClientFactory,
            UserServiceClient userServiceClient,
            EmailServiceClient emailServiceClient,
            RoomServiceClient roomServiceClient,
            IConfiguration configuration)
        {
            _reservationRepository = reservationRepository;
            _httpContextAccessor = httpContextAccessor;
            _logger = logger;
            _reservationStatusRepository = reservationStatusRepository;
            _httpClientFactory = httpClientFactory;
            _userServiceClient = userServiceClient;
            _emailServiceClient = emailServiceClient;
            _roomServiceClient = roomServiceClient;
            _configuration = configuration;
        }

        public async Task<IEnumerable<ReservationDTO>> GetAllReservations()
        {
            return await _reservationRepository.GetAllReservations();
        }

        public async Task<IEnumerable<ReservationDTO>> GetReservationsByEmail(string email)
        {
            return await _reservationRepository.GetReservationsByEmail(email);
        }

        public async Task<IEnumerable<ReservationDTO>> GetReservationsByRoomId(int roomId)
        {
            return await _reservationRepository.GetReservationsByRoomId(roomId);
        }

        public async Task<ReservationDTO?> GetReservation(int id)
        {
            var reservation = await _reservationRepository.GetReservationById(id);
            if (reservation == null)
            {
                return null;
            }
            
            return ReservationHelpers.ConvertToReservationDTO(reservation);
        }

        public async Task<IActionResult> PutReservation(int id, Reservation reservation)
        {
            if (id != reservation.ReservationId)
            {
                return new BadRequestResult();
            }

            var existingReservation = await _reservationRepository.GetReservationById(id);
            if (existingReservation == null)
            {
                return new NotFoundResult();
            }

            await _reservationRepository.UpdateReservation(reservation);
            return new NoContentResult();
        }

        public async Task<ActionResult<ReservationDTO>> PostReservation(CreateReservationDTO createReservationDto)
        {
            _logger.LogInformation("Reservation details received: {ReservationData}", JsonSerializer.Serialize(createReservationDto));

            var isRoomAvailable = await ReservationHelpers.IsRoomAvailable(createReservationDto.RoomId, createReservationDto.CheckInDate, createReservationDto.CheckOutDate, _reservationRepository);
            if (!isRoomAvailable)
            {
                return new BadRequestObjectResult("The room is not available for the selected dates.");
            }

            var userEmail = _httpContextAccessor.HttpContext?.User?.Identity?.Name;
            if (userEmail == null)
            {
                return new ForbidResult();
            }
            // Assume user exists since authenticated, avoid calling user service to prevent 403 issues
            var user = new UserDTO { Email = userEmail, FullName = "" };

            var room = await _roomServiceClient.GetRoomByIdAsync(createReservationDto.RoomId);
            if (room == null) return new BadRequestObjectResult("Room not found"); // Should be covered by IsRoomAvailable but good for safety

            var status = await _reservationStatusRepository.GetReservationStatusById(1); // 1 = pending
            if (status == null) return new BadRequestObjectResult("Status 'pending' not found");

            var reservation = new Reservation
            {
                RoomId = createReservationDto.RoomId,
                CheckInDate = createReservationDto.CheckInDate,
                CheckOutDate = createReservationDto.CheckOutDate,
                NumberOfGuests = createReservationDto.NumberOfGuests,
                Email = user.Email,
                NumberOfNights = (int)(createReservationDto.CheckOutDate - createReservationDto.CheckInDate).TotalDays,
                StatusId = 1,
                ReservationDate = System.DateTime.UtcNow,
                Status = status
            };

            await _reservationRepository.AddReservation(reservation);

            // Note: Confirmation email will be sent after payment is confirmed via StripeService

            var reservationDto = new ReservationDTO
            {
                ReservationId = reservation.ReservationId,
                StatusId = reservation.StatusId,
                Email = reservation.Email,
                User = new UserDTO
                {
                    Email = user.Email,
                    FullName = user.FullName
                },
                RoomId = reservation.RoomId,
                ReservationDate = reservation.ReservationDate,
                CheckInDate = reservation.CheckInDate,
                CheckOutDate = reservation.CheckOutDate,
                NumberOfNights = reservation.NumberOfNights,
                NumberOfGuests = reservation.NumberOfGuests
            };

            return reservationDto;
        }

        public async Task<IActionResult> ConfirmReservation(int id)
        {
            var reservation = await _reservationRepository.GetReservationById(id);
            if (reservation == null)
            {
                return new NotFoundResult();
            }

            if (reservation.StatusId != 1) // Only confirm if pending
            {
                return new BadRequestObjectResult("Reservation is not in pending status.");
            }

            reservation.StatusId = 2; // Assume 2 is confirmed
            reservation.Status = null!;

            await _reservationRepository.UpdateReservation(reservation);
            return new NoContentResult();
        }

        public async Task<IActionResult> DeleteReservation(int id)
        {
            var reservation = await _reservationRepository.GetReservationById(id);
            if (reservation == null)
            {
                return new NotFoundResult();
            }

            // Check if the reservation can be cancelled (it is not in cancelled or checked-in status)
            if (reservation.StatusId == 5 || reservation.StatusId == 3) // 5 = cancelled, 3 = checked-in
            {
                return new BadRequestObjectResult("The reservation cannot be cancelled because it is already cancelled or check-in has already taken place.");
            }

            // Check if the cancellation is being made at least 24 hours before check-in
            var now = DateTime.UtcNow;
            var timeUntilCheckIn = reservation.CheckInDate - now;
            if (timeUntilCheckIn.TotalHours < 24)
            {
                return new BadRequestObjectResult("The reservation can only be cancelled at least 24 hours before check-in.");
            }

            // Send cancellation email
            var user = await _userServiceClient.GetUserByEmailAsync(reservation.Email);
            if (user == null)
            {
                return new BadRequestObjectResult("The user associated with the reservation could not be found..");
            }
            string emailSubject = "Reservation Cancellation Confirmation";
            string emailBody = $@"
           <h1>Reservation Cancellation Confirmation</h1>
           <p>Dear {user.FullName},</p>
           <p>Your reservation has been successfully cancelled.</p>
           <p><strong>Reservation details:</strong></p>
           <ul>
               <li>Entry date: {reservation.CheckInDate:dd/MM/yyyy}</li>
               <li>Departure date: {reservation.CheckOutDate:dd/MM/yyyy}</li>
               <li>Number of nights: {reservation.NumberOfNights}</li>
               <li>Number of guests: {reservation.NumberOfGuests}</li>
           </ul>
           <p>If you have any questions or need further assistance, please contact us.</p>
       ";
            await _emailServiceClient.SendEmailAsync(user.Email!, emailSubject, emailBody, "cancellation", reservation.ReservationId);

            // Soft delete (Cancel)
            reservation.StatusId = 5; // Cancelled
            reservation.CancellationDate = DateTime.UtcNow;
            reservation.CancellationReason = "Cancelled by user";

            // Nullify navigation properties to avoid EF Core tracking conflicts
            // since we already have tracked entities in the context (e.g. user)
            // reservation.User = null!;  <-- Removed
            // reservation.Room = null!;  <-- Removed
            reservation.Status = null!;

            await _reservationRepository.UpdateReservation(reservation);
            return new NoContentResult();
        }
    }
}

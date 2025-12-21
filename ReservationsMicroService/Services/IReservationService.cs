using Microsoft.AspNetCore.Mvc;
using ReservationsMicroService.DTOs;
using ReservationsMicroService.Models;

namespace ReservationsMicroService.Services
{
  public interface IReservationService
  {
    Task<IEnumerable<ReservationDTO>> GetAllReservations();
    Task<IEnumerable<ReservationDTO>> GetReservationsByEmail(string email);
    Task<ReservationDTO?> GetReservation(int id);
    Task<IActionResult> PutReservation(int id, Reservation reservation);
    Task<ActionResult<ReservationDTO>> PostReservation(CreateReservationDTO createReservationDto);
    Task<IActionResult> DeleteReservation(int id);
    Task<IActionResult> ConfirmReservation(int id);
    Task<IEnumerable<ReservationDTO>> GetReservationsByRoomId(int roomId);
  }
}

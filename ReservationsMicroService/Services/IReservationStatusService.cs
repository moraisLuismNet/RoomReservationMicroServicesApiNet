using Microsoft.AspNetCore.Mvc;
using ReservationsMicroService.DTOs;

namespace ReservationsMicroService.Services
{
  public interface IReservationStatusService
  {
    Task<IEnumerable<ReservationStatusDTO>> GetAllReservationStatuses();
    Task<ActionResult<ReservationStatusDTO>> GetReservationStatus(int id);
    Task<IActionResult> PutReservationStatus(int id, UpdateReservationStatusDTO updateReservationStatusDto);
    Task<ActionResult<ReservationStatusDTO>> PostReservationStatus(CreateReservationStatusDTO createReservationStatusDto);
    Task<IActionResult> DeleteReservationStatus(int id);
  }
}

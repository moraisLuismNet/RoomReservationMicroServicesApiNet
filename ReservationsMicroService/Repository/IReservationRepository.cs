using ReservationsMicroService.DTOs;
using ReservationsMicroService.Models;

namespace ReservationsMicroService.Repository
{
  public interface IReservationRepository
  {
    Task<IEnumerable<ReservationDTO>> GetAllReservations();
    Task<IEnumerable<ReservationDTO>> GetReservationsByEmail(string email);
    Task<Reservation?> GetReservationById(int id);
    Task AddReservation(Reservation reservation);
    Task UpdateReservation(Reservation reservation);
    Task DeleteReservation(int id);
    Task<bool> ReservationExists(int id);
    Task<bool> IsRoomAvailable(int roomId, DateTime checkIn, DateTime checkOut);
    Task<IEnumerable<ReservationDTO>> GetReservationsByRoomId(int roomId);
    Task EnsureRoomExists(int roomId, string roomNumber);
  }
}

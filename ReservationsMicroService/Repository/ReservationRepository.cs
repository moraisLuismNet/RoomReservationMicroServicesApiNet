using Microsoft.EntityFrameworkCore;
using ReservationsMicroService.Data;
using ReservationsMicroService.DTOs;
using ReservationsMicroService.Models;

namespace ReservationsMicroService.Repository
{
  public class ReservationRepository : IReservationRepository
  {
    private readonly ReservationsDbContext _context;

    public ReservationRepository(ReservationsDbContext context)
    {
      _context = context;
    }

    public async Task<IEnumerable<ReservationDTO>> GetAllReservations()
    {
      var reservations = await _context.Reservations
          .Include(r => r.Status)
          .Include(r => r.Room)
          .ToListAsync();

      var reservationDtos = reservations.Select(r => new ReservationDTO
      {
        ReservationId = r.ReservationId,
        StatusId = r.StatusId,
        User = new UserDTO
        {
          Email = r.Email,
          FullName = string.Empty // FullName would be fetched from UserMicroservice in a real scenario
        },
        RoomId = r.RoomId,
        RoomNumber = r.Room?.RoomNumber,
        Room = new RoomDTO
        {
            RoomId = r.RoomId,
            RoomNumber = r.Room?.RoomNumber ?? string.Empty
        },
        ReservationDate = r.ReservationDate,
        CheckInDate = r.CheckInDate,
        CheckOutDate = r.CheckOutDate,
        NumberOfNights = r.NumberOfNights,
        NumberOfGuests = r.NumberOfGuests,
        CancellationDate = r.CancellationDate,
        CancellationReason = r.CancellationReason
      }).ToList();

      return reservationDtos;
    }

    public async Task<IEnumerable<ReservationDTO>> GetReservationsByEmail(string email)
    {
      var reservations = await _context.Reservations
          .Include(r => r.Status)
          .Include(r => r.Room)
          .Where(r => r.Email == email)
          .ToListAsync();

      var reservationDtos = reservations.Select(r => new ReservationDTO
      {
        ReservationId = r.ReservationId,
        StatusId = r.StatusId,
        User = new UserDTO
        {
          Email = r.Email,
          FullName = string.Empty
        },
        RoomId = r.RoomId,
        RoomNumber = r.Room?.RoomNumber,
        Room = new RoomDTO
        {
            RoomId = r.RoomId,
            RoomNumber = r.Room?.RoomNumber ?? string.Empty
        },
        ReservationDate = r.ReservationDate,
        CheckInDate = r.CheckInDate,
        CheckOutDate = r.CheckOutDate,
        NumberOfNights = r.NumberOfNights,
        NumberOfGuests = r.NumberOfGuests,
        CancellationDate = r.CancellationDate,
        CancellationReason = r.CancellationReason
      }).ToList();

      return reservationDtos;
    }

    public async Task<Reservation?> GetReservationById(int id)
    {
      return await _context.Reservations
          .Include(r => r.Room)
          .AsNoTracking()
          .FirstOrDefaultAsync(r => r.ReservationId == id);
    }

    public async Task AddReservation(Reservation reservation)
    {
      _context.Reservations.Add(reservation);
      await _context.SaveChangesAsync();
    }

    public async Task UpdateReservation(Reservation reservation)
    {
      _context.Entry(reservation).State = EntityState.Modified;
      await _context.SaveChangesAsync();
    }

    public async Task DeleteReservation(int id)
    {
      var reservation = await _context.Reservations.FindAsync(id);
      if (reservation != null)
      {
        _context.Reservations.Remove(reservation);
        await _context.SaveChangesAsync();
      }
    }

    public async Task<bool> ReservationExists(int id)
    {
      return await _context.Reservations.AnyAsync(e => e.ReservationId == id);
    }

    public async Task<bool> IsRoomAvailable(int roomId, DateTime checkIn, DateTime checkOut)
    {
      return !await _context.Reservations
          .AnyAsync(r => r.RoomId == roomId &&
                        r.StatusId != 5 && // 5 = cancelled
                        r.StatusId != 6 && // 6 = no-show
                        ((checkIn >= r.CheckInDate && checkIn < r.CheckOutDate) ||
                         (checkOut > r.CheckInDate && checkOut <= r.CheckOutDate) ||
                         (checkIn <= r.CheckInDate && checkOut >= r.CheckOutDate)));
    }

    public async Task<IEnumerable<ReservationDTO>> GetReservationsByRoomId(int roomId)
    {
        var reservations = await _context.Reservations
          .Include(r => r.Status)
          .Include(r => r.Room)
          .Where(r => r.RoomId == roomId && r.StatusId != 5 && r.StatusId != 6) // Exclude cancelled/no-show
          .ToListAsync();

        var reservationDtos = reservations.Select(r => new ReservationDTO
        {
            ReservationId = r.ReservationId,
            StatusId = r.StatusId,
            User = new UserDTO
            {
                Email = r.Email,
                FullName = string.Empty
            },
            RoomId = r.RoomId,
            RoomNumber = r.Room?.RoomNumber,
            Room = new RoomDTO
            {
                RoomId = r.RoomId,
                RoomNumber = r.Room?.RoomNumber ?? string.Empty
            },
            ReservationDate = r.ReservationDate,
            CheckInDate = r.CheckInDate,
            CheckOutDate = r.CheckOutDate,
            NumberOfNights = r.NumberOfNights,
            NumberOfGuests = r.NumberOfGuests,
            CancellationDate = r.CancellationDate,
            CancellationReason = r.CancellationReason
        }).ToList();

        return reservationDtos;
    }

    public async Task EnsureRoomExists(int roomId, string roomNumber)
    {
        var exists = await _context.Rooms.AnyAsync(r => r.RoomId == roomId);
        if (!exists)
        {
            _context.Rooms.Add(new Room { RoomId = roomId, RoomNumber = roomNumber });
            await _context.SaveChangesAsync();
        }
    }
  }
}

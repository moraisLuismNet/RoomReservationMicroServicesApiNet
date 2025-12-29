using Microsoft.EntityFrameworkCore;
using ReservationsMicroService.Models;

namespace ReservationsMicroService.Data
{
    public class ReservationsDbContext : DbContext
    {
        public ReservationsDbContext(DbContextOptions<ReservationsDbContext> options) : base(options)
        {
        }

        public DbSet<Reservation> Reservations { get; set; }
        public DbSet<ReservationStatus> ReservationStatuses { get; set; }
        public DbSet<Room> Rooms { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            // Initial data configuration for ReservationStatuses
            modelBuilder.Entity<ReservationStatus>().HasData(
                new ReservationStatus { StatusId = 1, Name = "pending" },
                new ReservationStatus { StatusId = 2, Name = "confirmed" },
                new ReservationStatus { StatusId = 3, Name = "checked-in" },
                new ReservationStatus { StatusId = 4, Name = "checked-out" },
                new ReservationStatus { StatusId = 5, Name = "cancelled" },
                new ReservationStatus { StatusId = 6, Name = "no-show" }
            );

            // Configuring CHECK restrictions for ReservationStatus
            modelBuilder.Entity<ReservationStatus>(entity =>
            {
                entity.ToTable(t => t.HasCheckConstraint("CK_ReservationStatus_Name", "Name IN ('pending', 'confirmed', 'checked-in', 'checked-out', 'cancelled', 'no-show')"));
            });
        }
    }
}

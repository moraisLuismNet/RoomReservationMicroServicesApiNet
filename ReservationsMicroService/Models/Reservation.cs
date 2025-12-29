using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ReservationsMicroService.Models
{
    public class Reservation
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int ReservationId { get; set; }

        [Required]
        public int StatusId { get; set; }

        [ForeignKey("StatusId")]
        public virtual ReservationStatus? Status { get; set; }

        [Required]
        public required string Email { get; set; }

        [Required]
        [Column("RoomId")]
        public int RoomId { get; set; }

        [ForeignKey("RoomId")]
        public virtual Room? Room { get; set; }

        public DateTime ReservationDate { get; set; } = DateTime.UtcNow;

        [Required]
        public DateTime CheckInDate { get; set; }

        [Required]
        public DateTime CheckOutDate { get; set; }

        [Required]
        public int NumberOfNights { get; set; }

        [Required]
        public int NumberOfGuests { get; set; }

        public DateTime? CancellationDate { get; set; }

        [StringLength(500)]
        public string CancellationReason { get; set; } = string.Empty;
    }
}

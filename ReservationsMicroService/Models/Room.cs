using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ReservationsMicroService.Models
{
    public class Room
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.None)] // Id managed by RoomMicroService
        public int RoomId { get; set; }

        [Required]
        [StringLength(10)]
        public required string RoomNumber { get; set; }
    }
}

using System.ComponentModel.DataAnnotations;

namespace ReservationsMicroService.DTOs
{
  public class UpdateReservationStatusDTO
  {
    [Required]
    [StringLength(50)]
    public required string Name { get; set; }
  }
}

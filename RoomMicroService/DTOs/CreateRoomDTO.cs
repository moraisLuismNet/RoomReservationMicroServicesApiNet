using System.ComponentModel.DataAnnotations;

namespace RoomMicroService.DTOs
{
  public class CreateRoomDTO
  {
    [Required]
    [StringLength(10)]
    public required string RoomNumber { get; set; }

    [Required]
    public int RoomTypeId { get; set; }

    public bool IsActive { get; set; } = true;
    public string? ImageRoom { get; set; }
  }
}

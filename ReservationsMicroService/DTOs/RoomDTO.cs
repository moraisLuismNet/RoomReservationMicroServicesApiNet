namespace ReservationsMicroService.DTOs
{
    public class RoomDTO
    {
        public int RoomId { get; set; }
        public required string RoomNumber { get; set; }
        public int RoomTypeId { get; set; }
        public string? RoomTypeName { get; set; }
        public decimal PricePerNight { get; set; }
        public string? Description { get; set; }
        public int Capacity { get; set; }
        public bool IsActive { get; set; }
    }
}

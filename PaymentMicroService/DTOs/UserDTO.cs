namespace PaymentMicroService.DTOs
{
    public class UserDTO
    {
        public required string Email { get; set; }
        public required string FullName { get; set; }
        public string? PhoneNumber { get; set; }
        public bool IsActive { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? LastLogin { get; set; }
        public string? Role { get; set; }
    }
}

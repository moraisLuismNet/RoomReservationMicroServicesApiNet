using System.ComponentModel.DataAnnotations;

namespace UserAndAuthorizationManagementMicroService.DTOs
{
    public class UpdateUserDTO
    {
        [StringLength(200)]
        public string? FullName { get; set; }

        [StringLength(20)]
        public string? PhoneNumber { get; set; }

        public bool? IsActive { get; set; }
    }
}

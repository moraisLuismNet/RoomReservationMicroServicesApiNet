using Microsoft.AspNetCore.Mvc;
using UserAndAuthorizationManagementMicroService.DTOs;

namespace UserAndAuthorizationManagementMicroService.Services
{
  public interface IUserService
  {
    Task<IEnumerable<UserDTO>> GetAllUsers();
    Task<UserDTO?> GetUser(string email);
    Task<IActionResult> PutUser(string email, UpdateUserDTO updateUserDto);
    Task<ActionResult<UserDTO>> PostUser(CreateUserDTO createUserDto);
    Task<IActionResult> DeleteUser(string email);
  }
}

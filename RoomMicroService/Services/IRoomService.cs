using Microsoft.AspNetCore.Mvc;
using RoomMicroService.DTOs;

namespace RoomMicroService.Services
{
  public interface IRoomService
  {
    Task<IEnumerable<RoomDTO>> GetAllRooms();
    Task<ActionResult<RoomDTO>> GetRoom(int id);
    Task<IActionResult> PutRoom(int id, UpdateRoomDTO updateRoomDto);
    Task<ActionResult<RoomDTO>> PostRoom(CreateRoomDTO createRoomDto);
    Task<IActionResult> DeleteRoom(int id);
  }
}

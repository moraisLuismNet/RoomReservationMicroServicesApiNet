using Microsoft.AspNetCore.Mvc;
using RoomMicroService.DTOs;
using RoomMicroService.Models;
using RoomMicroService.Repository;

namespace RoomMicroService.Services
{
  public class RoomService : IRoomService
  {
    private readonly IRoomRepository _roomRepository;
    private readonly IRoomTypeRepository _roomTypeRepository;

    public RoomService(IRoomRepository roomRepository, IRoomTypeRepository roomTypeRepository)
    {
      _roomRepository = roomRepository;
      _roomTypeRepository = roomTypeRepository;
    }

    public async Task<IEnumerable<RoomDTO>> GetAllRooms()
    {
      var rooms = await _roomRepository.GetAllRooms();
      var roomDtos = rooms.Select(r => new RoomDTO
      {
        RoomId = r.RoomId,
        RoomNumber = r.RoomNumber,
        RoomTypeId = r.RoomTypeId,
        RoomTypeName = r.RoomType?.RoomTypeName ?? string.Empty,
        PricePerNight = r.RoomType?.PricePerNight ?? 0,
        Description = r.RoomType?.Description ?? string.Empty,
        Capacity = r.RoomType?.Capacity ?? 0,
        IsActive = r.IsActive,
        ImageRoom = r.ImageRoom
      }).ToList();

      return roomDtos;
    }

    public async Task<ActionResult<RoomDTO>> GetRoom(int id)
    {
      var room = await _roomRepository.GetRoomById(id);
      if (room == null)
      {
        return new NotFoundResult();
      }

      var roomDto = new RoomDTO
      {
        RoomId = room.RoomId,
        RoomNumber = room.RoomNumber,
        RoomTypeId = room.RoomTypeId,
        RoomTypeName = room.RoomType?.RoomTypeName ?? string.Empty,
        PricePerNight = room.RoomType?.PricePerNight ?? 0,
        Description = room.RoomType?.Description ?? string.Empty,
        Capacity = room.RoomType?.Capacity ?? 0,
        IsActive = room.IsActive,
        ImageRoom = room.ImageRoom
      };

      return roomDto;
    }

    public async Task<IActionResult> PutRoom(int id, UpdateRoomDTO updateRoomDto)
    {
      var existingRoom = await _roomRepository.GetRoomById(id);
      if (existingRoom == null)
      {
        return new NotFoundResult();
      }

      existingRoom.RoomNumber = updateRoomDto.RoomNumber;
      existingRoom.RoomTypeId = updateRoomDto.RoomTypeId;
      existingRoom.IsActive = updateRoomDto.IsActive;
      existingRoom.ImageRoom = updateRoomDto.ImageRoom;

      await _roomRepository.UpdateRoom(existingRoom);

      // Get the updated room to return it
      var updatedRoom = await _roomRepository.GetRoomById(id);
      if (updatedRoom == null)
      {
        return new NotFoundResult();
      }

      var roomDto = new RoomDTO
      {
        RoomId = updatedRoom.RoomId,
        RoomNumber = updatedRoom.RoomNumber,
        RoomTypeId = updatedRoom.RoomTypeId,
        RoomTypeName = updatedRoom.RoomType?.RoomTypeName ?? string.Empty,
        PricePerNight = updatedRoom.RoomType?.PricePerNight ?? 0,
        Description = updatedRoom.RoomType?.Description ?? string.Empty,
        Capacity = updatedRoom.RoomType?.Capacity ?? 0,
        IsActive = updatedRoom.IsActive,
        ImageRoom = updatedRoom.ImageRoom
      };

      return new OkObjectResult(roomDto);
    }

    public async Task<ActionResult<RoomDTO>> PostRoom(CreateRoomDTO createRoomDto)
    {
      var roomType = await _roomTypeRepository.GetRoomTypeById(createRoomDto.RoomTypeId);
      if (roomType == null)
      {
        return new NotFoundResult();
      }

      var room = new Room
      {
        RoomNumber = createRoomDto.RoomNumber,
        RoomTypeId = createRoomDto.RoomTypeId,
        IsActive = createRoomDto.IsActive,
        ImageRoom = createRoomDto.ImageRoom,
        RoomType = roomType
      };

      await _roomRepository.AddRoom(room);

      var createdRoom = await _roomRepository.GetRoomById(room.RoomId);
      if (createdRoom == null)
      {
        return new NotFoundResult();
      }

      var roomDto = new RoomDTO
      {
        RoomId = createdRoom.RoomId,
        RoomNumber = createdRoom.RoomNumber,
        RoomTypeId = createdRoom.RoomTypeId,
        RoomTypeName = createdRoom.RoomType?.RoomTypeName ?? string.Empty,
        PricePerNight = createdRoom.RoomType?.PricePerNight ?? 0,
        Description = createdRoom.RoomType?.Description ?? string.Empty,
        Capacity = createdRoom.RoomType?.Capacity ?? 0,
        IsActive = createdRoom.IsActive,
        ImageRoom = createdRoom.ImageRoom
      };

      return roomDto;
    }

    public async Task<IActionResult> DeleteRoom(int id)
    {
      var room = await _roomRepository.GetRoomById(id);
      if (room == null)
      {
        return new NotFoundResult();
      }

      await _roomRepository.DeleteRoom(id);
      return new NoContentResult();
    }
  }
}

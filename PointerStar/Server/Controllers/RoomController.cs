using HashidsNet;
using Microsoft.AspNetCore.Mvc;
using PointerStar.Server.Room;
using PointerStar.Shared;

namespace PointerStar.Server.Controllers;

[ApiController]
[Route("api/[controller]")]
public class RoomController : ControllerBase
{
    private static readonly Random Random = new();
    private Hashids Hashids { get; }
    private IRoomManager RoomManager { get; }

    public RoomController(Hashids hashids, IRoomManager roomManager)
    {
        Hashids = hashids ?? throw new ArgumentNullException(nameof(hashids));
        RoomManager = roomManager ?? throw new ArgumentNullException(nameof(roomManager));
    }


    [HttpGet("Generate")]
    public string Generate()
        => Hashids.Encode(Random.Next());

    [HttpGet("GetNewUserRole/{RoomId}")]
    public Task<Role> GetNewUserRole(string roomId)
        => RoomManager.GetNewUserRoleAsync(roomId);
}

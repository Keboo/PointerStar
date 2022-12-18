using HashidsNet;
using Microsoft.AspNetCore.Mvc;

namespace PointerStar.Server.Controllers;

[ApiController]
[Route("api/[controller]")]
public class RoomController : ControllerBase
{
    private static readonly Random Random = new();
    private Hashids Hashids { get; }
    
    public RoomController(Hashids hashids)
    {
        Hashids = hashids ?? throw new ArgumentNullException(nameof(hashids));
    }


    [HttpGet("Generate")]
    public string Generate()
    {
        return Hashids.Encode(Random.Next());
    }
}

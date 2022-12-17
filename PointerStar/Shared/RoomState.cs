namespace PointerStar.Shared;

public record class RoomState(string RoomId, User[] Users);

public record class User(Guid Id, string Name, string? Vote);


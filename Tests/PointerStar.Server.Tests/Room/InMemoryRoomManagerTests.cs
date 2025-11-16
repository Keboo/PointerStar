using PointerStar.Server.Room;

namespace PointerStar.Server.Tests.Room;

// Removed [ConstructorTests] attribute since InMemoryRoomManager now has optional dependencies
public partial class InMemoryRoomManagerTests : RoomManagerTests<InMemoryRoomManager>
{
    [Fact]
    public void InMemoryRoomManagerConstructor_WithNullTelemetryClient_CreatesInstance()
    {
        // TelemetryClient is nullable/optional, so null is allowed
        var manager = new InMemoryRoomManager(null);
        Assert.NotNull(manager);
    }
}

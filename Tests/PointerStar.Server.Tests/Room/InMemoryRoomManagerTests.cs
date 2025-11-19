using PointerStar.Server.Room;

namespace PointerStar.Server.Tests.Room;

[ConstructorTests(typeof(InMemoryRoomManager))]
public partial class InMemoryRoomManagerTests : RoomManagerTests<InMemoryRoomManager>
{
    partial void AutoMockerTestSetup(AutoMocker mocker, string testName)
        => mocker.WithApplicationInsights();
}

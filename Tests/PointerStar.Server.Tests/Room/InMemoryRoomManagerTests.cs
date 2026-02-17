using Microsoft.ApplicationInsights;
using Microsoft.ApplicationInsights.Extensibility;
using PointerStar.Server.Room;

namespace PointerStar.Server.Tests.Room;

[ConstructorTests(typeof(InMemoryRoomManager))]
public partial class InMemoryRoomManagerTests : RoomManagerTests<InMemoryRoomManager>
{
    partial void AutoMockerTestSetup(AutoMocker mocker, string testName)
    {
        var telemetryConfig = new TelemetryConfiguration();
        var telemetryClient = new TelemetryClient(telemetryConfig);
        mocker.Use(telemetryClient);
    }
}

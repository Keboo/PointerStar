using Microsoft.ApplicationInsights;
using Microsoft.ApplicationInsights.Extensibility;
using PointerStar.Server.Room;

namespace PointerStar.Server.Tests.Room;

// ConstructorTests attribute removed due to incompatibility with ApplicationInsights 3.0.0
// The Moq.AutoMocker.Generators source generator doesn't support ApplicationInsights 3.0.0
public class InMemoryRoomManagerTests : RoomManagerTests<InMemoryRoomManager>
{
}

# PointerStar - AI Coding Instructions

## Architecture Overview

PointerStar is a **real-time pointing poker application** built with Blazor WebAssembly + ASP.NET Core Server using SignalR for real-time communication. The solution follows a clean **3-tier architecture**:

- **Server**: ASP.NET Core with SignalR Hub (`RoomHub`)
- **Client**: Blazor WebAssembly with MudBlazor UI
- **Shared**: Common models and interfaces

## Key Architectural Patterns

### MVVM with CommunityToolkit.Mvvm
- ViewModels inherit from `ViewModelBase` (which extends `ObservableObject`)
- Use `[ObservableProperty]` attributes for auto-generated properties
- Components inherit from `ComponentBase<TViewModel>` for automatic state binding
- Example: `RoomViewModel` drives `Room.razor` page

### SignalR Real-time Communication
- **Hub**: `RoomHub` manages room state and user connections
- **Client Interface**: `IRoomHubConnection` abstracts SignalR client
- **Method Naming**: Uses constants from `RoomHubConnection` (e.g., `JoinRoomMethodName`)
- **State Flow**: All room updates broadcast via `RoomUpdatedMethodName`

### Room Management Pattern
- **IRoomManager**: Abstracts room operations (currently `InMemoryRoomManager`)
- **Connection Tracking**: Maps ConnectionId → RoomId → User
- **Concurrency**: Uses `SemaphoreSlim` per room for thread-safe operations
- **State Immutability**: Uses C# records with `with` expressions

## Critical Developer Workflows

### Build & Test
```powershell
# Build entire solution
dotnet build --configuration Release

# Run all tests with coverage
dotnet test --configuration Release --collect:"XPlat Code Coverage"
```

### Project Structure Rules
- **Central Package Management**: All NuGet versions in `Directory.Packages.props`
- **Global Usings**: Common imports in `Directory.Build.props`
- **Test Conventions**: Use `Moq.AutoMock` for dependency injection, `AutoMockerMixins` for HTTP mocking

### SignalR Development
- Hub methods must use `[HubMethodName(RoomHubConnection.ConstantName)]`
- Always check `Context.ConnectionId` for user authorization
- Broadcast to groups: `await Clients.Groups(roomId).SendAsync(...)`

## Project-Specific Conventions

### Role-Based Security
```csharp
// Only facilitators can modify room settings
if (currentUser.Role == Role.Facilitator) 
{
    // Allow room modifications
}
```

### User State Management
- **Client-Side**: Cookies store last room/role via `ICookie` service
- **Server-Side**: ConnectionId maps to User and Room state
- **Auto-reconnect**: Client attempts to rejoin last room with cached credentials

### Testing Patterns
- Abstract test base: `RoomManagerTests<TRoomManager>` for implementation-agnostic tests
- HTTP mocking: `AutoMockerMixins.SetupHttpCall()` for client HTTP tests
- Room creation helper: `CreateRoom(sut, connectionIds)` for multi-user scenarios

## Key Files to Understand

### Core Architecture
- `Server/Program.cs` - SignalR configuration and DI setup
- `Server/Hubs/RoomHub.cs` - Real-time communication hub  
- `Server/Room/InMemoryRoomManager.cs` - Room state management
- `Shared/RoomState.cs` - Immutable state records

### Client Patterns
- `Client/ViewModels/RoomViewModel.cs` - Main page logic with timer and state
- `Client/Components/ComponentBase.cs` - MVVM binding base class
- `Client/ViewModels/ViewModelBase.cs` - Observable base with lifecycle

### Testing Infrastructure
- `Tests/Directory.Build.props` - Test-specific configurations
- `Tests/PointerStar.Client.Tests/AutoMockerMixins.Http.cs` - HTTP mocking extensions
- `Tests/PointerStar.Server.Tests/Room/RoomManagerTests.cs` - Abstract test patterns

## Integration Points

### SignalR Message Flow
1. Client → `IRoomHubConnection.JoinRoomAsync(roomId, user)`
2. Hub → `RoomHub.JoinRoomAsync()` via `[HubMethodName]`
3. Manager → `IRoomManager.AddUserToRoomAsync()`
4. Broadcast → All clients in room receive updated `RoomState`

### State Synchronization
- **Cookie Persistence**: Room, role, and name cached client-side
- **Auto-reconnect**: On page load, attempt rejoin with cached data
- **Real-time Updates**: All room changes propagate immediately via SignalR

### External Dependencies
- **HashidsNet**: Room ID obfuscation (TODO: needs environment salt)
- **MudBlazor**: UI component library with specific snackbar configuration
- **Toolbelt.Blazor.PWA.Updater**: Progressive Web App support
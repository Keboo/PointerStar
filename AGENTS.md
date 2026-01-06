# PointerStar - AI Agent Instructions

This file provides practical guidance for AI coding agents working on PointerStar.

## Project Overview

PointerStar is a real-time pointing poker application built with:
- **Backend**: ASP.NET Core with SignalR for real-time communication
- **Frontend**: Blazor WebAssembly with MudBlazor UI components
- **Architecture**: 3-tier (Client, Server, Shared) with MVVM pattern

## Development Environment Setup

### Prerequisites
- .NET SDK (version specified in `global.json` if present, otherwise latest)
- IDE: Visual Studio, Visual Studio Code, or Rider

### Building the Project
```bash
# Build entire solution
dotnet build --configuration Release

# Build specific project
dotnet build PointerStar/Server/PointerStar.Server.csproj
dotnet build PointerStar/Client/PointerStar.Client.csproj
```

### Running the Application
```bash
# Run the server (includes client)
dotnet run --project PointerStar/Server/PointerStar.Server.csproj
```

## Testing Instructions

### Running Tests
```bash
# Run all tests
dotnet test --configuration Release

# Run all tests with code coverage
dotnet test --configuration Release --collect:"XPlat Code Coverage"

# Run specific test project
dotnet test Tests/PointerStar.Server.Tests/PointerStar.Server.Tests.csproj
dotnet test Tests/PointerStar.Client.Tests/PointerStar.Client.Tests.csproj
dotnet test Tests/PointerStar.Shared.Tests/PointerStar.Shared.Tests.csproj
```

### Testing Conventions
- Use `Moq.AutoMock` for dependency injection in tests
- Use `AutoMockerMixins` for HTTP mocking in client tests
- Follow existing test patterns in `RoomManagerTests<TRoomManager>` for implementation-agnostic tests
- Helper methods like `CreateRoom(sut, connectionIds)` are available for multi-user test scenarios

### Before Committing
1. Ensure all tests pass: `dotnet test`
2. Build in Release mode: `dotnet build --configuration Release`
3. Check that no new warnings are introduced

## Project Structure & Dependencies

### Central Package Management
- All NuGet package versions are defined in `Directory.Packages.props`
- Use `<PackageReference Include="PackageName" />` without version in project files
- Update versions centrally in `Directory.Packages.props`

### Global Usings
- Common imports are defined in `Directory.Build.props`
- Avoid redundant `using` statements for commonly imported namespaces

### Project Organization
```
PointerStar/
├── Client/          # Blazor WebAssembly client
├── Server/          # ASP.NET Core server with SignalR
├── Shared/          # Shared models and interfaces
Tests/
├── PointerStar.Client.Tests/
├── PointerStar.Server.Tests/
├── PointerStar.Shared.Tests/
```

## Coding Conventions

### MVVM Pattern (Client)
- ViewModels inherit from `ViewModelBase` (which extends `ObservableObject`)
- Use `[ObservableProperty]` attributes for auto-generated properties from CommunityToolkit.Mvvm
- Razor components inherit from `ComponentBase<TViewModel>` for automatic state binding
- Example: `RoomViewModel` drives the `Room.razor` page

### SignalR Development (Server)
- Hub methods must use `[HubMethodName(RoomHubConnection.ConstantName)]` attribute
- Always verify `Context.ConnectionId` for user authorization
- Broadcast updates: `await Clients.Groups(roomId).SendAsync(...)`
- Use constant method names from `RoomHubConnection` class

### State Management
- Server: Use immutable C# records with `with` expressions for state updates
- Client: Store user preferences in cookies via `ICookie` service
- Room state: Thread-safe operations using `SemaphoreSlim` per room

### Role-Based Security
```csharp
// Only facilitators can modify room settings
if (currentUser.Role == Role.Facilitator) 
{
    // Allow room modifications
}
```

## Key Files to Reference

### Server Architecture
- `PointerStar/Server/Program.cs` - SignalR and DI configuration
- `PointerStar/Server/Hubs/RoomHub.cs` - Real-time communication hub
- `PointerStar/Server/Room/InMemoryRoomManager.cs` - Room state management

### Client Architecture  
- `PointerStar/Client/ViewModels/RoomViewModel.cs` - Main room page logic
- `PointerStar/Client/Components/ComponentBase.cs` - MVVM binding base
- `PointerStar/Client/ViewModels/ViewModelBase.cs` - Observable base with lifecycle

### Shared Models
- `PointerStar/Shared/RoomState.cs` - Immutable state records
- `PointerStar/Shared/IRoomHubConnection.cs` - SignalR client interface

## Common Tasks

### Adding a New NuGet Package
1. Add package reference WITHOUT version to the .csproj file:
   ```xml
   <PackageReference Include="PackageName" />
   ```
2. Add version to `Directory.Packages.props`:
   ```xml
   <PackageVersion Include="PackageName" Version="x.y.z" />
   ```

### Adding a New SignalR Method
1. Add constant in `RoomHubConnection`: `public const string NewMethodName = "NewMethod";`
2. Add method to `IRoomHubConnection` interface
3. Implement in `RoomHub` with `[HubMethodName(RoomHubConnection.NewMethodName)]`
4. Implement client-side in `RoomHubConnection`

### Adding a New ViewModel Property
1. Add `[ObservableProperty]` field to ViewModel:
   ```csharp
   [ObservableProperty]
   private string myProperty = string.Empty;
   ```
2. CommunityToolkit.Mvvm auto-generates the `MyProperty` public property
3. Bind in Razor component: `@ViewModel.MyProperty`

## Pull Request Guidelines

### PR Title Format
Use conventional commit style:
- `feat: Add new feature`
- `fix: Fix bug description`  
- `refactor: Refactor description`
- `test: Add tests for feature`
- `docs: Update documentation`

### PR Checklist
- [ ] All tests pass locally
- [ ] Code builds without warnings in Release mode
- [ ] New code follows existing patterns (MVVM, SignalR conventions)
- [ ] Tests added/updated for new functionality
- [ ] No secrets or sensitive data committed
- [ ] Thread-safety considered for shared state

## Additional Notes

### External Dependencies
- **HashidsNet**: Used for room ID obfuscation
- **MudBlazor**: UI component library - check existing usage for snackbar configuration
- **Toolbelt.Blazor.PWA.Updater**: Progressive Web App support

### Known TODOs
- HashidsNet needs environment-specific salt configuration

### When Stuck
1. Check the existing `.github/copilot-instructions.md` for architectural patterns
2. Look at similar existing code in the codebase
3. Review test files for examples of mocking and test setup
4. Ensure you understand the SignalR message flow: Client → Hub → Manager → Broadcast

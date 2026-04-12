# PointerStar - AI Coding Instructions

## Architecture Overview

PointerStar is a **real-time pointing poker application** built with:

- **Local orchestration**: .NET Aspire AppHost with the JavaScript/Vite hosting integration
- **Frontend**: React + TypeScript + Vite + Material UI
- **Backend**: ASP.NET Core with SignalR
- **Shared**: C# room models and SignalR method-name constants

The ASP.NET Core server continues to host the built SPA and the backend APIs from the same app. The Aspire AppHost is used for local orchestration and development-time Vite hosting.

## Key Architectural Patterns

### Aspire orchestration
- The AppHost lives in `PointerStar/AppHost`
- Use the AppHost as the default local entry point
- The AppHost runs the ASP.NET Core server and `ClientApp` via `AddViteApp`
- The server still builds and serves the static SPA for publish/deployment

### React frontend
- The app lives in `PointerStar/ClientApp`
- Use React Router for routes
- Use Material UI and MUI Icons to preserve the material design look
- Keep browser persistence in small service modules (cookies, local storage, theme, telemetry)
- Use `@microsoft/signalr` for realtime room communication

### SignalR realtime flow
- **Hub**: `PointerStar/Server/Hubs/RoomHub.cs`
- **Method names**: `PointerStar/Shared/RoomHubConnection.cs`
- **State flow**: clients invoke room actions on the hub and receive `RoomUpdated` broadcasts

### Room management
- `IRoomManager` abstracts server-side room operations
- `InMemoryRoomManager` stores room state and user connections
- Room updates use immutable records and `with` expressions

## Build and test

```powershell
# Frontend dependencies
npm ci --prefix PointerStar/ClientApp

# Frontend tests
npm run test:run --prefix PointerStar/ClientApp

# Full build
dotnet build --configuration Release

# Backend/shared tests
dotnet test --configuration Release
```

## Project structure

```text
PointerStar/
├── AppHost/     # Aspire AppHost for local orchestration
├── ClientApp/   # React client
├── Server/      # ASP.NET Core host, APIs, SignalR hub
└── Shared/      # Shared room models and hub method names
Tests/
├── PointerStar.Server.Tests/
└── PointerStar.Shared.Tests/
```

## Conventions

- Keep frontend code in TypeScript
- Prefer small, explicit service modules over implicit global state
- Preserve the existing room routes and backend contracts unless the task requires a coordinated backend change
- Hub methods should continue using constants from `RoomHubConnection`
- Only facilitators may change room settings or remove users

## Key files

- `PointerStar/AppHost/Program.cs`
- `PointerStar/ClientApp/src/pages/RoomPage.tsx`
- `PointerStar/ClientApp/src/services/roomHubClient.ts`
- `PointerStar/Server/Program.cs`
- `PointerStar/Server/Hubs/RoomHub.cs`
- `PointerStar/Server/Room/InMemoryRoomManager.cs`
- `PointerStar/Shared/RoomState.cs`
- `PointerStar/Shared/RoomHubConnection.cs`

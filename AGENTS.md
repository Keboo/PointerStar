# PointerStar - AI Agent Instructions

## Project Overview

PointerStar is a real-time pointing poker application with:

- **Frontend**: React + TypeScript + Vite + Material UI
- **Backend**: ASP.NET Core with SignalR
- **Local orchestration**: .NET Aspire AppHost with the JavaScript/Vite hosting integration
- **Architecture**: `AppHost`, `ClientApp`, `Server`, `Shared`

The ASP.NET Core server still hosts the built frontend assets and the backend endpoints from the same deployment unit. The Aspire AppHost is the preferred local development entry point.

## Development setup

### Prerequisites
- .NET SDK 10
- Node.js

### Build
```powershell
npm ci --prefix PointerStar/ClientApp
npm run test:run --prefix PointerStar/ClientApp
dotnet build --configuration Release
```

### Run
```powershell
dotnet run --project PointerStar/AppHost/PointerStar.AppHost.csproj
```

## Structure

```text
PointerStar/
├── AppHost/     # Aspire AppHost for local orchestration
├── ClientApp/   # React SPA
├── Server/      # ASP.NET Core host, controllers, SignalR hub
└── Shared/      # Shared room models and hub method constants
Tests/
├── PointerStar.Server.Tests/
└── PointerStar.Shared.Tests/
```

## Frontend conventions

- Use React function components and TypeScript
- Use Material UI / MUI Icons for visual consistency
- Prefer running the client through the Aspire AppHost; use direct Vite startup only when a task specifically needs it
- Keep browser persistence in dedicated services:
  - cookies and consent
  - recent rooms in local storage
  - theme preference
  - telemetry bootstrap
- Use `@microsoft/signalr` for realtime room actions
- Preserve the existing routes:
  - `/`
  - `/room/:roomId`

## Backend conventions

- Hub methods must use `[HubMethodName(RoomHubConnection.ConstantName)]`
- Keep room state immutable on the server
- Only facilitators can modify room settings or remove users

## Key files

- `PointerStar/AppHost/Program.cs`
- `PointerStar/ClientApp/src/pages/HomePage.tsx`
- `PointerStar/ClientApp/src/pages/RoomPage.tsx`
- `PointerStar/ClientApp/src/services/roomHubClient.ts`
- `PointerStar/Server/Program.cs`
- `PointerStar/Server/Hubs/RoomHub.cs`
- `PointerStar/Server/Room/InMemoryRoomManager.cs`
- `PointerStar/Shared/RoomState.cs`
- `PointerStar/Shared/RoomHubConnection.cs`

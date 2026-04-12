# Pointer*

A simple real-time pointing poker app with a **React** frontend, an **ASP.NET Core + SignalR** backend, and a **.NET Aspire AppHost** for local orchestration.

## Architecture

- **PointerStar/ClientApp** - React + TypeScript + Vite + Material UI frontend, with `PointerStar.ClientApp.esproj` for Solution Explorer support
- **PointerStar/AppHost** - .NET Aspire AppHost that runs the server project and Vite dev server together
- **PointerStar/Server** - ASP.NET Core host, controllers, SignalR hub, and room management
- **PointerStar/Shared** - Shared room models and SignalR method-name constants

The backend still hosts the built frontend assets in production. The server build/publish flow generates the React app and copies the output into ASP.NET, while the Aspire AppHost uses the JavaScript/Vite integration for local development.

Visual Studio users can open the client as its own JavaScript project in Solution Explorer via `PointerStar/ClientApp/PointerStar.ClientApp.esproj`. That project is for local development ergonomics; production hosting still goes through `PointerStar/Server`.

## Build

```powershell
# Install frontend dependencies
npm ci --prefix PointerStar/ClientApp

# Run frontend tests
npm run test:run --prefix PointerStar/ClientApp

# Build the full solution, including the AppHost
dotnet build --configuration Release
```

## Run locally with Aspire

```powershell
dotnet run --project PointerStar/AppHost/PointerStar.AppHost.csproj
```

## Publish the hosted app

```powershell
dotnet publish PointerStar/Server/PointerStar.Server.csproj --configuration Release
```

## Test

```powershell
# Frontend
npm run test:run --prefix PointerStar/ClientApp

# Backend and shared projects
dotnet test --configuration Release
```

## Direct frontend development

```powershell
# If you run the backend directly, point Vite at that server URL
$env:VITE_BACKEND_URL = "https://localhost:7017"
npm run dev --prefix PointerStar/ClientApp
```

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
pnpm install --prefix PointerStar/ClientApp

# Run frontend tests
pnpm run test:run --prefix PointerStar/ClientApp

# Build the full solution, including the AppHost
dotnet build --configuration Release
```

## Run locally with Aspire

```powershell
dotnet run --project PointerStar/AppHost/PointerStar.AppHost.csproj
```

For local Giphy search support, set the API key in **user secrets** for the server project:

```powershell
cd PointerStar/Server
dotnet user-secrets set "Giphy:ApiKey" "<your-giphy-api-key>"
```

Production deployments read `GIPHY_API_KEY` from the app's environment, which is set by the GitHub Actions `GIPHY_API_KEY` secret during deploy.

## Publish the hosted app

```powershell
dotnet publish PointerStar/Server/PointerStar.Server.csproj --configuration Release
```

## Test

```powershell
# Frontend
pnpm run test:run --prefix PointerStar/ClientApp

# Backend and shared projects
dotnet test --configuration Release
```

## Direct frontend development

```powershell
# If you run the backend directly, point Vite at that server URL
$env:VITE_BACKEND_URL = "https://localhost:7017"
pnpm run dev --prefix PointerStar/ClientApp
```

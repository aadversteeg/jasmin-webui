# CLAUDE.md - jasmin-webui

## Running the Application

- Start the Blazor WebAssembly app:
  ```bash
  cd src && dotnet run --project Core.Infrastructure.BlazorApp/Core.Infrastructure.BlazorApp.csproj --urls "http://localhost:5001"
  ```
- The app runs on **port 5001**: http://localhost:5001
- The app connects to a Jasmin server on `http://localhost:5000` by default

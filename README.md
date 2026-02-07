# jasmin-webui

A Blazor WebAssembly application for managing and monitoring MCP (Model Context Protocol) servers via [jasmin-server](https://github.com/aadversteeg/jasmin-server).

## Features

### Event Viewer
- Real-time event streaming via Server-Sent Events (SSE)
- Filter events by server name and event type
- Expandable event cards with detailed information
- Auto-scroll to latest events

### MCP Server Management
- View all configured MCP servers with status indicators
- Add new MCP servers with configuration testing
- Edit existing server configurations
- Delete servers with confirmation
- Filter servers by name

### Server Details
- **Configuration Tab**: View and copy server configuration (command, args, environment variables)
- **Tools Tab**: Browse available tools with parameter schemas, invoke tools directly
- **Prompts Tab**: Browse available prompts, invoke prompts with argument input
- **Resources Tab**: Browse and view server resources with deep-linking support

### Instance Management
- Start and stop server instances on demand
- View running instances with start time
- Multiple lifecycle modes:
  - Per-invocation (start, invoke, stop)
  - Per-dialog (instance lives while dialog is open)
  - Persistent (instance survives dialog close)
  - Reuse existing instances

### Tool & Prompt Invocation
- Schema-based input forms with validation
- JSON input mode for complex parameters
- Invocation history with navigation
- Copy results to clipboard

## Architecture

```
src/
├── Core.Domain/                    # Domain models and events
├── Core.Application/               # Service interfaces
├── Core.Infrastructure.BlazorApp/  # Blazor WASM application
│   ├── Components/                 # Reusable Razor components
│   ├── Pages/                      # Routable pages
│   └── ViewModels/                 # MVVM ViewModels
├── Core.Infrastructure.JasminClient/   # HTTP client for jasmin-server API
└── Core.Infrastructure.LocalStorage/   # Browser localStorage persistence
```

The application uses the MVVM pattern with [Blazing.Mvvm](https://github.com/AathifMahir/Blazing.Mvvm) and [CommunityToolkit.Mvvm](https://github.com/CommunityToolkit/dotnet).

## Prerequisites

- [.NET 10 SDK](https://dotnet.microsoft.com/download)
- [jasmin-server](https://github.com/aadversteeg/jasmin-server) running on port 5000 (default)

## Getting Started

1. Clone the repository:
   ```bash
   git clone https://github.com/aadversteeg/jasmin-webui.git
   cd jasmin-webui
   ```

2. Build the solution:
   ```bash
   cd src
   dotnet build
   ```

3. Run the application:
   ```bash
   dotnet run --project Core.Infrastructure.BlazorApp --urls "http://localhost:5001"
   ```

4. Open your browser and navigate to `http://localhost:5001`

5. Click **Configure** to set the jasmin-server URL (default: `http://localhost:5000`)

## Running Tests

```bash
cd src
dotnet test
```

## Configuration

The application stores user preferences in browser localStorage, including:
- Panel widths and visibility
- Event filters (server selection, event types)
- Tool invocation history
- Instance lifecycle preferences per server

## Development

### Project Structure

| Project | Description |
|---------|-------------|
| `Core.Domain` | Domain models, events, and value objects |
| `Core.Application` | Service interfaces and application contracts |
| `Core.Infrastructure.BlazorApp` | Blazor WebAssembly UI with components and ViewModels |
| `Core.Infrastructure.JasminClient` | HTTP client implementation for jasmin-server REST API |
| `Core.Infrastructure.LocalStorage` | Browser localStorage service for persisting preferences |

### Key Components

- **EventCard**: Displays individual MCP server events
- **ToolInvocationDialog**: Modal for invoking tools with parameter input
- **PromptInvocationDialog**: Modal for invoking prompts with argument input
- **ResourceViewer**: Displays resource content with syntax highlighting
- **InstanceSelector**: Dropdown for selecting instance lifecycle mode
- **SchemaBasedInput**: Dynamic form generation based on JSON schema

## License

MIT License - see [LICENSE](LICENSE) for details.

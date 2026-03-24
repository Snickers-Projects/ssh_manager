# SSH Manager

A portable Windows desktop application for managing and connecting to SSH sessions. Built with WPF and .NET Framework 4.8, it provides a full terminal experience using xterm.js and ships as a single executable.

## Features

- **Full terminal emulation** — xterm.js-powered terminal with colors, cursor positioning, Unicode, and support for interactive programs (vim, htop, etc.)
- **Tabbed interface** — Multiple SSH sessions open simultaneously in tabs
- **Session management** — Save, search, group, and annotate SSH sessions
- **Authentication** — Password (saved with DPAPI encryption), password (prompt each time), or private key
- **Saved commands** — Store frequently used commands globally or per-session, execute with a click
- **Import/export** — Back up and restore sessions and commands as JSON
- **Configurable** — Terminal font size, scrollback buffer, default SSH port/username, window size, confirm-on-close
- **Portable** — All data stored in a `Data/` folder next to the executable (no registry or %APPDATA% usage)
- **Single executable** — All dependencies embedded into one ~1.4 MB .exe via Costura.Fody

## System Requirements

### To run the application

- **Windows 10** (version 1803+) or **Windows 11**
- **.NET Framework 4.8** — Pre-installed on Windows 10 (May 2019 Update and later) and all versions of Windows 11
- **Microsoft Edge WebView2 Runtime** — Pre-installed on Windows 11 and Windows 10 (via Windows Updates since 2021). If missing, download from [Microsoft](https://developer.microsoft.com/en-us/microsoft-edge/webview2/#download)

### To build from source

Everything above, plus:

- **Git** — [git-scm.com/downloads/win](https://git-scm.com/downloads/win)
- **.NET SDK 6.0 or later** — Any modern .NET SDK can build .NET Framework 4.8 projects. Download from [dotnet.microsoft.com/download](https://dotnet.microsoft.com/download)
- **.NET Framework 4.8 Developer Pack** — Provides the reference assemblies needed to compile against .NET Framework 4.8. Download from [Microsoft](https://dotnet.microsoft.com/en-us/download/dotnet-framework/net48) (look for "Developer Pack", not "Runtime")

## Building

### Clone and build

```powershell
git clone <repository-url>
cd ssh_manager
dotnet build SshManager.sln
```

### Run in debug mode

```powershell
dotnet run --project SshManager\SshManager.csproj
```

### Publish a release build

```powershell
dotnet publish SshManager\SshManager.csproj -c Release
```

The output is at `SshManager\bin\Release\net48\publish\`. The only required file is `SshManager.exe` (and optionally `SshManager.exe.config`). All NuGet DLLs (managed and native) are embedded inside the executable by Costura.Fody.

## Project Structure

```
ssh_manager/
├── SshManager.sln                        # Solution file
├── .github/
│   └── copilot-instructions.md           # AI agent context for this project
└── SshManager/
    ├── SshManager.csproj                 # Project file (net48 target, NuGet references, MSBuild targets)
    ├── FodyWeavers.xml                   # Costura.Fody config for native DLL embedding
    ├── App.xaml / App.xaml.cs            # Application entry point
    ├── Helpers/
    │   ├── AppPaths.cs                   # Portable data folder paths
    │   ├── PasswordHelper.cs             # DPAPI encrypt/decrypt
    │   └── TerminalHtmlBuilder.cs        # Builds xterm.js HTML page from embedded resources
    ├── Models/
    │   ├── SshSession.cs                 # Session data model + AuthMethod enum
    │   ├── SavedCommand.cs               # Saved command model (global or per-session)
    │   └── AppSettings.cs                # Application settings model
    ├── Resources/
    │   └── Terminal/
    │       ├── xterm.js                  # xterm.js v4.19.0 (embedded resource)
    │       ├── xterm.css                 # xterm.js styles (embedded resource)
    │       └── xterm-addon-fit.js        # Auto-fit addon (embedded resource)
    ├── Services/
    │   ├── ISessionStorageService.cs     # Interface: session + command persistence
    │   ├── JsonSessionStorageService.cs  # JSON file implementation (sessions.json, commands.json)
    │   ├── ISshConnectionService.cs      # Interface: SSH connections
    │   ├── SshConnectionService.cs       # SSH.NET implementation
    │   ├── SettingsService.cs            # Settings persistence (settings.json)
    │   ├── IImportExportProvider.cs      # Interface: import/export providers
    │   ├── SshManagerJsonImportExportProvider.cs  # Native JSON import/export
    │   └── ExportData.cs                 # Export container model
    ├── ViewModels/
    │   ├── BaseViewModel.cs              # INotifyPropertyChanged base class
    │   ├── RelayCommand.cs               # ICommand implementation
    │   ├── MainViewModel.cs              # Session list, tab management, command execution
    │   └── TerminalTabViewModel.cs       # Per-tab SSH lifecycle (connect, send, resize, disconnect)
    └── Views/
        ├── MainWindow.xaml/.cs           # Main window: sidebar + tabbed terminal area
        ├── TerminalTabView.xaml/.cs      # WebView2/xterm.js terminal UserControl
        ├── CachedTabControl.cs           # Custom TabControl that keeps all tabs alive
        ├── SessionEditDialog.xaml/.cs    # Add/Edit session dialog
        ├── PasswordPromptDialog.xaml/.cs # Connect-time password prompt
        ├── SettingsDialog.xaml/.cs       # Application settings dialog
        ├── CommandManagerDialog.xaml/.cs  # Saved commands manager
        ├── CommandEditDialog.xaml/.cs    # Add/Edit command dialog
        └── ImportExportDialog.xaml/.cs   # Import/export sessions + commands
```

## Architecture

The application follows the **MVVM** (Model-View-ViewModel) pattern with a pragmatic approach — services are behind interfaces, but dependency injection is done manually (no DI framework).

### Data flow

```
SSH Server ←→ SSH.NET ShellStream ←→ TerminalTabViewModel ←→ TerminalTabView ←→ WebView2 ←→ xterm.js
```

- **SSH → Screen:** ShellStream fires DataReceived → ViewModel raises event → View calls `xterm.js term.write()` via WebView2
- **Keyboard → SSH:** xterm.js `onData` → WebView2 `postMessage` → View forwards to ViewModel → ShellStream.Write
- **Resize:** xterm.js `onResize` → WebView2 `postMessage` → View → ViewModel → SSH.NET channel resize (via reflection)

### Key design decisions

- **CachedTabControl** — WPF's default TabControl destroys and recreates tab content when switching tabs. This custom subclass keeps all tabs alive via visibility toggling, which is critical because each tab hosts a WebView2 instance that would lose its state if destroyed.
- **Embedded resources** — xterm.js/CSS/fit-addon are compiled into the assembly as embedded resources. `TerminalHtmlBuilder` reads these at runtime and assembles a self-contained HTML page with all JS/CSS inlined.
- **Costura.Fody** — All managed NuGet DLLs are embedded as compressed resources inside the .exe. Native WebView2Loader.dll (x86 + x64) is also embedded via the Unmanaged32/64Assemblies feature. An MSBuild target in the .csproj copies native DLLs from the NuGet cache into `costura32/costura64` folders before build.
- **Portable storage** — `AppPaths.DataFolder` returns a `Data/` folder next to the executable. All JSON files (sessions, commands, settings) and the WebView2 user data cache live here.

## Making Changes

### Adding a new authentication method

1. Add a value to the `AuthMethod` enum in [SshManager/Models/SshSession.cs](SshManager/Models/SshSession.cs)
2. Handle the new method in [SshManager/Services/SshConnectionService.cs](SshManager/Services/SshConnectionService.cs) — the `Connect` methods build `ConnectionInfo` objects with different authentication methods
3. Update the auth type selector in [SshManager/Views/SessionEditDialog.xaml](SshManager/Views/SessionEditDialog.xaml) and its code-behind to show/hide relevant fields
4. If the method needs connect-time input (like PromptPassword), update the `PasswordPromptFunc` delegate wiring in [SshManager/Views/MainWindow.xaml.cs](SshManager/Views/MainWindow.xaml.cs)

### Adding a new setting

1. Add a property with a default value to [SshManager/Models/AppSettings.cs](SshManager/Models/AppSettings.cs)
2. Add the corresponding UI control in [SshManager/Views/SettingsDialog.xaml](SshManager/Views/SettingsDialog.xaml) and wire it in the code-behind
3. Existing JSON without the new field will deserialize with the default — no migration needed

### Adding a new import/export format

1. Create a new class implementing `IImportExportProvider` in the `Services/` folder (see [SshManager/Services/SshManagerJsonImportExportProvider.cs](SshManager/Services/SshManagerJsonImportExportProvider.cs) for reference)
2. Register it in the provider list in [SshManager/Views/ImportExportDialog.xaml.cs](SshManager/Views/ImportExportDialog.xaml.cs)

### Adding a new dialog

1. Create a new `Window` XAML + code-behind in `Views/`
2. Open it from the appropriate location (sidebar button → MainWindow code-behind, or context-specific)
3. Follow the existing pattern: dialogs are modal (`ShowDialog()`), return data via public properties, and are consumed by the view code-behind which updates the ViewModel

### Changing the terminal emulator

The terminal is isolated behind three files:
- [SshManager/Helpers/TerminalHtmlBuilder.cs](SshManager/Helpers/TerminalHtmlBuilder.cs) — Builds the HTML page
- [SshManager/Views/TerminalTabView.xaml.cs](SshManager/Views/TerminalTabView.xaml.cs) — Bridges WebView2 ↔ ViewModel
- [SshManager/Resources/Terminal/](SshManager/Resources/Terminal/) — The xterm.js files (embedded resources)

To upgrade xterm.js, replace the files in `Resources/Terminal/` and update `TerminalHtmlBuilder` if the API changed.

### Modifying the session data model

1. Edit [SshManager/Models/SshSession.cs](SshManager/Models/SshSession.cs)
2. Update [SshManager/Views/SessionEditDialog.xaml](SshManager/Views/SessionEditDialog.xaml) for the UI
3. Existing `sessions.json` files will still load — Newtonsoft.Json ignores missing properties and uses defaults

### Adding service implementations

All services are behind interfaces (`ISessionStorageService`, `ISshConnectionService`, `IImportExportProvider`). To swap an implementation:
1. Create a new class implementing the interface
2. Change the instantiation in the consuming code (currently in view code-behind files, mainly [SshManager/Views/MainWindow.xaml.cs](SshManager/Views/MainWindow.xaml.cs))

## Dependencies

| Package | Version | Purpose |
|---------|---------|---------|
| [SSH.NET](https://github.com/sshnet/SSH.NET) | 2024.1.0 | SSH protocol implementation |
| [Newtonsoft.Json](https://www.newtonsoft.com/json) | 13.0.3 | JSON serialization for session/command/settings persistence |
| [Microsoft.Web.WebView2](https://developer.microsoft.com/en-us/microsoft-edge/webview2/) | 1.0.2903.40 | Hosts xterm.js terminal in WPF |
| [Costura.Fody](https://github.com/Fody/Costura) | 5.7.0 | Embeds all DLLs into single executable |
| [Fody](https://github.com/Fody/Fody) | 6.8.0 | IL weaving engine (required by Costura) |

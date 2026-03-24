# SSH Manager - Project Reference

## Overview
Portable WPF desktop application for managing and connecting to SSH sessions.
Targets **.NET Framework 4.8** for maximum Windows compatibility (pre-installed on Win 10/11).
Ships as a single executable via Costura.Fody assembly embedding.

## Tech Stack
- **Language:** C# (.NET Framework 4.8)
- **UI Framework:** WPF (Windows Presentation Foundation)
- **Terminal:** xterm.js v4.19.0 (bundled as embedded resource) via WebView2 (pre-installed on Win 10/11)
- **Architecture:** MVVM (Model-View-ViewModel)
- **SSH Library:** SSH.NET (Renci.SshNet) v2024.1.0
- **Serialization:** Newtonsoft.Json v13.0.3
- **Password Security:** Windows DPAPI (System.Security.Cryptography.ProtectedData)
- **Assembly Embedding:** Costura.Fody v5.7.0 + Fody v6.8.0 (all managed + native DLLs embedded into single exe)

## Project Structure
```
ssh_manager/
├── SshManager.sln                       # Visual Studio solution file
├── .github/
│   └── copilot-instructions.md          # This file (agent context)
├── .gitignore
└── SshManager/
    ├── SshManager.csproj                # Project file (net48, NuGet refs, MSBuild targets)
    ├── FodyWeavers.xml                  # Costura config (unmanaged DLL embedding)
    ├── App.xaml / App.xaml.cs           # Application entry point
    ├── Helpers/
    │   ├── AppPaths.cs                  # Portable data folder (Data/ next to exe)
    │   ├── PasswordHelper.cs            # DPAPI encrypt/decrypt
    │   └── TerminalHtmlBuilder.cs       # Builds xterm.js HTML from embedded resources
    ├── Models/
    │   ├── SshSession.cs                # Session data model (AuthMethod enum: Password/PrivateKey/PromptPassword)
    │   ├── SavedCommand.cs              # Saved command model (global or per-session scope)
    │   └── AppSettings.cs               # Application settings model
    ├── Resources/
    │   └── Terminal/
    │       ├── xterm.js                 # xterm.js v4.19.0 (embedded resource)
    │       ├── xterm.css                # xterm.js styles (embedded resource)
    │       └── xterm-addon-fit.js       # Auto-fit addon (embedded resource)
    ├── Services/
    │   ├── ISessionStorageService.cs    # Storage interface (sessions + commands)
    │   ├── JsonSessionStorageService.cs # JSON file persistence (sessions.json, commands.json)
    │   ├── ISshConnectionService.cs     # SSH connection interface
    │   ├── SshConnectionService.cs      # SSH.NET implementation
    │   ├── SettingsService.cs           # Settings persistence (settings.json)
    │   ├── IImportExportProvider.cs     # Import/export provider interface
    │   ├── SshManagerJsonImportExportProvider.cs  # Native JSON import/export
    │   └── ExportData.cs                # Export container (sessions + commands)
    ├── ViewModels/
    │   ├── BaseViewModel.cs             # INotifyPropertyChanged base
    │   ├── RelayCommand.cs              # ICommand implementation
    │   ├── MainViewModel.cs             # Session list, tab management, commands
    │   └── TerminalTabViewModel.cs      # Single SSH terminal tab lifecycle
    └── Views/
        ├── MainWindow.xaml/.cs          # Main window (sidebar + tabbed terminals)
        ├── TerminalTabView.xaml/.cs      # WebView2/xterm.js terminal UserControl
        ├── CachedTabControl.cs          # Tab control that keeps all tabs alive
        ├── SessionEditDialog.xaml/.cs   # Add/Edit session dialog
        ├── PasswordPromptDialog.xaml/.cs # Connect-time password prompt
        ├── SettingsDialog.xaml/.cs       # Application settings dialog
        ├── CommandManagerDialog.xaml/.cs # Saved commands manager
        ├── CommandEditDialog.xaml/.cs    # Add/Edit command dialog
        └── ImportExportDialog.xaml/.cs  # Import/export sessions + commands
```

## Key Features
- Full xterm.js terminal (colors, cursor, Unicode, vim/htop support)
- Searchable session list (live search-as-you-type)
- Tabbed SSH terminal interface (tab name = session name)
- Password (saved) / Password (prompt each time) / Private Key authentication
- DPAPI-encrypted password storage
- Open in external PowerShell window option
- Saved commands (global or per-session scope)
- Session import/export (extensible provider pattern)
- Configurable settings (window size, terminal font/scrollback, SSH defaults, confirm-on-close)
- Portable storage — all data in Data/ folder next to executable
- Single-exe distribution (all DLLs embedded via Costura.Fody)
- Session grouping and notes
- Connection status indicators (green dot = connected)

## Build Commands
- **Build:** `dotnet build SshManager.sln`
- **Run:** `dotnet run --project SshManager\SshManager.csproj`
- **Publish:** `dotnet publish SshManager\SshManager.csproj -c Release`
  - Output: `SshManager\bin\Release\net48\publish\SshManager.exe` (single file, ~1.4 MB)

## Architecture Notes

### MVVM Pattern
- Services are behind interfaces for testability and swappability
- View code-behind handles dialog interactions (pragmatic MVVM — no DI framework)
- TerminalTabViewModel manages SSH lifecycle per tab
- MainViewModel owns session list and tab collection
- DataTemplates auto-select TerminalTabView for tab content

### Terminal Data Flow
- **SSH → Screen:** ShellStream → TerminalTabViewModel event → TerminalTabView → xterm.js (via WebView2 ExecuteScriptAsync)
- **User → SSH:** xterm.js onData → WebView2 PostMessage → TerminalTabView → TerminalTabViewModel → ShellStream
- **Resize:** xterm.js onResize → WebView2 PostMessage → TerminalTabView → TerminalTabViewModel → reflection on SSH.NET private `_channel.SendWindowChangeRequest`

### Embedded Resources
- xterm.js, xterm.css, xterm-addon-fit.js bundled as embedded resources in the assembly
- TerminalHtmlBuilder reads these at runtime and assembles HTML with inlined JS/CSS
- Font size and scrollback are injected via string replacement in the HTML template

### Single-Exe Packaging
- Costura.Fody embeds all managed NuGet DLLs into the exe as compressed resources
- Native WebView2Loader.dll (x86 + x64) embedded via Costura's Unmanaged32/64Assemblies feature
- MSBuild target `CopyCosturaNativeDlls` copies native DLLs from NuGet cache into costura32/costura64 folders before build

### Portable Storage
- AppPaths.DataFolder returns `Data/` folder next to executable
- Sessions, commands, and settings stored as JSON files in this folder
- WebView2 user data also stored under `Data/WebView2/`
- No registry or %APPDATA% usage

### Tab Isolation
- CachedTabControl (custom TabControl subclass) keeps all tab content alive via visibility toggling
- Default WPF TabControl destroys/recreates content when switching tabs — this would kill WebView2 instances

# SafeScriptStudio (WPF)

A dark-themed WPF script workspace UI with local-only actions.

## Features
- Sidebar layout inspired by modern editor dashboards.
- Script list panel with sample entries.
- Multi-tab script editing area.
- `RUN LOCAL` button updates status text.
- `SAVE` button writes script files to `Documents/SafeScriptStudio`.
- `INJECT TO TERMINAL` now supports two safe modes:
  - **Pipe mode**: sends extracted `print(...)` messages to **Safe Script Terminal** via named pipe.
  - **Plugin DLL mode**: loads local `SafeScriptPlugin.dll` and calls `SafeScriptPlugin.Entry.Execute(string script)` then forwards its result to terminal.

## Build locally
```bash
dotnet restore SafeScriptStudio.sln
dotnet build SafeScriptStudio/SafeScriptStudio.csproj -c Release
dotnet build SafeTerminalHost/SafeTerminalHost.csproj -c Release
```

## Build EXE in GitHub Actions
The workflow `.github/workflows/build-wpf.yml` publishes:
- `SafeScriptStudio.exe` (WPF GUI)
- `SafeTerminalHost.exe` (terminal companion)
- `SafeScriptPlugin.dll` (plugin for **Plugin DLL mode**, included by default)

All outputs are uploaded in artifact `SafeScriptStudio-win-x64`.


## Troubleshooting
- If `SafeScriptStudio.exe` does not open, check log file: `%LocalAppData%\SafeScriptStudio\logs\app.log`.
- Ensure `SafeTerminalHost.exe` is next to `SafeScriptStudio.exe` in the same folder when using `INJECT TO TERMINAL`.


## Plugin DLL mode contract
- `SafeScriptPlugin.dll` is included in the CI artifact by default. If missing, place it next to `SafeScriptStudio.exe`.
- Provide a public static method: `string SafeScriptPlugin.Entry.Execute(string script)`.
- This is local plugin loading only (not process injection).

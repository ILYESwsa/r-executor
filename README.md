# SafeScriptStudio (WPF)

A dark-themed WPF script workspace UI with local-only actions.

## Features
- Sidebar layout inspired by modern editor dashboards.
- Script list panel with sample entries.
- Multi-tab script editing area.
- `RUN LOCAL` button updates status text.
- `SAVE` button writes script files to `Documents/SafeScriptStudio`.
- `INJECT TO TERMINAL` launches/targets a companion app named **Safe Script Terminal** and sends extracted `print(...)` messages to it through a local named pipe.

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

Both are uploaded in artifact `SafeScriptStudio-win-x64`.


## Troubleshooting
- If `SafeScriptStudio.exe` does not open, check log file: `%LocalAppData%\SafeScriptStudio\logs\app.log`.
- Ensure `SafeTerminalHost.exe` is next to `SafeScriptStudio.exe` in the same folder when using `INJECT TO TERMINAL`.

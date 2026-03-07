# SafeScriptStudio (WPF)

A dark-themed WPF script workspace UI with local-only actions.

## Features
- Sidebar layout inspired by modern editor dashboards.
- Script list panel with sample entries.
- Large script editing area.
- `RUN LOCAL` button updates status text (no injection, no game process access).
- `SAVE` button writes script files to `Documents/SafeScriptStudio`.

## Build locally
```bash
dotnet restore SafeScriptStudio/SafeScriptStudio.csproj
dotnet build SafeScriptStudio/SafeScriptStudio.csproj -c Release
```

## Build EXE in GitHub Actions
The workflow `.github/workflows/build-wpf.yml` publishes a self-contained single-file Windows build and uploads it as an artifact named `SafeScriptStudio-win-x64`.

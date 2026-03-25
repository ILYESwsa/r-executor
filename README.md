# Mega Windows Performance Tweaks Utility (WPF + WebView2)

Windows-only desktop app using C#/.NET WPF with Discord OAuth2 authentication via WebView2. After successful login, the app switches to a native tweak utility interface (WebView is hidden).

## Project Structure

- `DiscordOAuthWpf.csproj`
- `App.xaml`
- `App.xaml.cs`
- `MainWindow.xaml`
- `MainWindow.xaml.cs`
- `DiscordOAuthHandler.cs`
- `WindowsTweaksService.cs`
- `.env.example`
- `.github/workflows/build.yml`

## Environment Variables

Create `.env` in the project root:

```env
CLIENT_ID=your_discord_client_id
CLIENT_SECRET=your_discord_client_secret
REDIRECT_URI=http://localhost:3000/callback
SESSION_SECRET=replace_with_a_long_random_string
```

Do not commit secrets.

## Discord App Setup

1. Open Discord Developer Portal: https://discord.com/developers/applications
2. Create app and configure OAuth2 redirect URL:
   - `http://localhost:3000/callback`
3. Copy client credentials into `.env`.

## Local Run

```bash
dotnet run --project DiscordOAuthWpf.csproj
```

If login succeeds, the embedded browser is hidden and account details are shown in a native tweak utility layout. Tweak actions become available only after authentication.

## Build Single EXE

```bash
dotnet publish DiscordOAuthWpf.csproj -c Release -r win-x64 -p:PublishSingleFile=true -p:SelfContained=true -o dist
```

## GitHub Actions

Workflow builds on `windows-latest`, publishes single-file EXE, and uploads `dist/` artifact.

Release publish enables single-file compression (trimming is intentionally disabled because WPF publish with trimming fails with NETSDK1168).

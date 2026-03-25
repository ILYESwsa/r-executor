# Discord OAuth2 Electron Desktop App

## Project Structure

- `package.json`
- `main.js` (Electron entry)
- `server.js` (Express backend + Discord OAuth2)
- `.env.example`
- `.github/workflows/build.yml`

## Setup

1. Install Node.js 18+
2. Install dependencies:

```bash
npm install
```

3. Create `.env` from `.env.example`:

```bash
cp .env.example .env
```

4. Fill in values:

- `CLIENT_ID`
- `CLIENT_SECRET`
- `REDIRECT_URI`
- `SESSION_SECRET`
- `PORT` (optional, default `3000`)

## Create Discord Application

1. Open Discord Developer Portal: https://discord.com/developers/applications
2. Create a new application.
3. Go to **OAuth2** settings.
4. Add redirect URL:
   - `http://localhost:3000/callback`
5. Copy **Client ID** and **Client Secret** into `.env`.

## Run Locally

```bash
npm start
```

This starts Electron, launches the local Express server, opens `http://localhost:3000`, and requires Discord login before access.

## Build Windows .exe Locally

```bash
npm run build
```

Output is generated in `dist/`.

## GitHub Actions Build

On every push, workflow `.github/workflows/build.yml`:

1. Runs on `windows-latest`
2. Installs Node.js 18
3. Runs `npm install`
4. Runs `npm run build`
5. Uploads `.exe` from `dist/` as artifact

## Download .exe from GitHub Actions

1. Push code to GitHub.
2. Open repository **Actions** tab.
3. Open latest successful **Build Windows EXE** run.
4. Download artifact named **windows-exe**.

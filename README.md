# Discord OAuth2 Electron Desktop App

## Project Structure

- `package.json`
- `package-lock.json`
- `.env.example`
- `.gitignore`
- `env-loader.js`
- `server.js`
- `main.js`
- `.github/workflows/build.yml`

## Environment Variables

Create a `.env` file and set:

- `CLIENT_ID`
- `CLIENT_SECRET`
- `REDIRECT_URI`
- `SESSION_SECRET`
- `PORT` (optional, defaults to `3000`)

Supported `.env` lookup order:

1. `DOTENV_PATH` (if set)
2. current working directory (`.env`)
3. app directory (`.env`)
4. executable directory (`.env`)

## Discord App Setup

1. Open: https://discord.com/developers/applications
2. Create an application.
3. In OAuth2 settings, add redirect URL:
   - `http://localhost:3000/callback`
4. Copy Client ID and Client Secret into `.env`.

## Local Start

```bash
npm start
```

- Electron launches.
- Express server starts automatically.
- App opens at `http://localhost:3000`.
- User must authenticate with Discord before reaching `/app`.

## Build

```bash
npm run build
```

- Uses `electron-builder`
- Produces a Windows **portable** executable in `dist/`

## GitHub Actions

Workflow file: `.github/workflows/build.yml`

On every push, it:

1. Runs on `windows-latest`
2. Installs Node.js 18
3. Runs `npm install`
4. Runs `npm run build` with `GH_TOKEN` explicitly empty
5. Uploads `dist/` as an artifact

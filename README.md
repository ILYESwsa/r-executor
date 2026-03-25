# Discord OAuth2 Electron Desktop App

## Project Structure

- `package.json`
- `package-lock.json`
- `.env.example`
- `.gitignore`
- `server.js`
- `main.js`
- `.github/workflows/build.yml`

## Environment Variables

Create a `.env` file from `.env.example` and set:

- `CLIENT_ID`
- `CLIENT_SECRET`
- `REDIRECT_URI`
- `SESSION_SECRET`
- `PORT` (optional, defaults to `3000`)

## Discord App Setup

1. Open: https://discord.com/developers/applications
2. Create an application
3. In OAuth2 settings, add redirect URL: `http://localhost:3000/callback`
4. Copy Client ID and Client Secret into `.env`

## Local Start

```bash
npm start

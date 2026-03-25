require('dotenv').config();

const express = require('express');
const session = require('express-session');

const DISCORD_API_BASE = 'https://discord.com/api/v10';

function requireEnv(name) {
  const value = process.env[name];
  if (!value || String(value).trim() === '') {
    throw new Error(`Missing required environment variable: ${name}`);
  }
  return value;
}

function htmlPage(title, body) {
  return `<!doctype html>
<html lang="en">
  <head>
    <meta charset="utf-8" />
    <meta name="viewport" content="width=device-width, initial-scale=1" />
    <title>${title}</title>
    <style>
      :root { color-scheme: dark; }
      body {
        margin: 0;
        font-family: Arial, sans-serif;
        background: #121318;
        color: #f5f7ff;
        min-height: 100vh;
        display: grid;
        place-items: center;
      }
      .card {
        width: min(92vw, 540px);
        padding: 24px;
        border-radius: 14px;
        background: #1b1d26;
        box-shadow: 0 8px 30px rgba(0, 0, 0, 0.3);
      }
      .button {
        display: inline-block;
        padding: 12px 18px;
        border-radius: 10px;
        background: #5865f2;
        color: white;
        text-decoration: none;
        font-weight: 700;
      }
      .muted { color: #b7bdd1; }
    </style>
  </head>
  <body>
    <main class="card">${body}</main>
  </body>
</html>`;
}

function createServer() {
  requireEnv('CLIENT_ID');
  requireEnv('CLIENT_SECRET');
  requireEnv('REDIRECT_URI');
  const sessionSecret = requireEnv('SESSION_SECRET');

  const app = express();

  app.use(
    session({
      secret: sessionSecret,
      resave: false,
      saveUninitialized: false,
      cookie: {
        httpOnly: true,
        sameSite: 'lax',
        secure: false
      }
    })
  );

  app.get('/', (_req, res) => {
    res.send(
      htmlPage(
        'Discord OAuth2 Login',
        `
        <h1>Desktop Login</h1>
        <p class="muted">Authenticate with Discord to access the app.</p>
        <a class="button" href="/login">Login with Discord</a>
      `
      )
    );
  });

  app.get('/login', (_req, res) => {
    const params = new URLSearchParams({
      client_id: process.env.CLIENT_ID,
      redirect_uri: process.env.REDIRECT_URI,
      response_type: 'code',
      scope: 'identify'
    });

    res.redirect(`https://discord.com/oauth2/authorize?${params.toString()}`);
  });

  app.get('/callback', async (req, res) => {
    const { code } = req.query;

    if (!code) {
      res.status(400).send('Missing OAuth2 authorization code.');
      return;
    }

    try {
      const tokenResponse = await fetch(`${DISCORD_API_BASE}/oauth2/token`, {
        method: 'POST',
        headers: {
          'Content-Type': 'application/x-www-form-urlencoded'
        },
        body: new URLSearchParams({
          client_id: process.env.CLIENT_ID,
          client_secret: process.env.CLIENT_SECRET,
          grant_type: 'authorization_code',
          code: String(code),
          redirect_uri: process.env.REDIRECT_URI
        }).toString()
      });

      if (!tokenResponse.ok) {
        res.status(401).send('Failed to exchange authorization code for token.');
        return;
      }

      const tokenData = await tokenResponse.json();

      const userResponse = await fetch(`${DISCORD_API_BASE}/users/@me`, {
        headers: {
          Authorization: `Bearer ${tokenData.access_token}`
        }
      });

      if (!userResponse.ok) {
        res.status(401).send('Failed to fetch Discord user profile.');
        return;
      }

      const user = await userResponse.json();
      req.session.user = {
        id: user.id,
        username: user.username,
        avatar: user.avatar
      };

      res.redirect('/app');
    } catch {
      res.status(500).send('OAuth callback failed.');
    }
  });

  app.get('/app', (req, res) => {
    if (!req.session.user) {
      res.redirect('/');
      return;
    }

    const avatar = req.session.user.avatar
      ? `https://cdn.discordapp.com/avatars/${req.session.user.id}/${req.session.user.avatar}.png`
      : '';

    res.send(
      htmlPage(
        'Authenticated App',
        `
        <h1>Welcome, ${req.session.user.username}</h1>
        <p class="muted">Discord ID: ${req.session.user.id}</p>
        ${avatar ? `<img src="${avatar}" alt="avatar" width="64" height="64" style="border-radius: 50%;" />` : ''}
        <p style="margin-top: 16px;"><a class="button" href="/logout">Logout</a></p>
      `
      )
    );
  });

  app.get('/me', (req, res) => {
    if (!req.session.user) {
      res.status(401).json({ authenticated: false });
      return;
    }

    res.json({ authenticated: true, user: req.session.user });
  });

  app.get('/logout', (req, res) => {
    req.session.destroy(() => {
      res.redirect('/');
    });
  });

  return app;
}

function startServer() {
  const port = Number(process.env.PORT || 3000);
  const app = createServer();
  return app.listen(port, () => {
    console.log(`Server listening on http://localhost:${port}`);
  });
}

if (require.main === module) {
  startServer();
}

module.exports = {
  createServer,
  startServer
};

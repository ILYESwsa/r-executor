require('dotenv').config();

const express = require('express');
const session = require('express-session');

const DISCORD_API_BASE = 'https://discord.com/api';
const PORT = process.env.PORT || 3000;

function requireEnv(name) {
  const value = process.env[name];
  if (!value) {
    throw new Error(`Missing required environment variable: ${name}`);
  }
  return value;
}

function createServer() {
  const app = express();

  app.use(
    session({
      secret: process.env.SESSION_SECRET || 'replace-this-in-production',
      resave: false,
      saveUninitialized: false,
      cookie: {
        httpOnly: true,
        sameSite: 'lax',
        secure: false
      }
    })
  );

  app.get('/', (req, res) => {
    if (!req.session.user) {
      res.send(`<!doctype html>
<html>
  <head>
    <meta charset="utf-8" />
    <title>Discord Login</title>
    <style>
      body { font-family: Arial, sans-serif; background: #121212; color: #fff; display: grid; place-items: center; min-height: 100vh; }
      .card { background: #1e1e1e; padding: 24px; border-radius: 12px; width: 360px; text-align: center; }
      a { display: inline-block; margin-top: 12px; text-decoration: none; background: #5865F2; color: #fff; padding: 12px 16px; border-radius: 8px; font-weight: bold; }
    </style>
  </head>
  <body>
    <div class="card">
      <h1>Desktop Login</h1>
      <p>Please authenticate with Discord to continue.</p>
      <a href="/login">Login with Discord</a>
    </div>
  </body>
</html>`);
      return;
    }

    res.send(`<!doctype html>
<html>
  <head>
    <meta charset="utf-8" />
    <title>Dashboard</title>
    <style>
      body { font-family: Arial, sans-serif; background: #121212; color: #fff; margin: 0; padding: 32px; }
      .card { background: #1e1e1e; padding: 24px; border-radius: 12px; max-width: 520px; }
      img { width: 64px; height: 64px; border-radius: 50%; }
      a { color: #9aa4ff; }
    </style>
  </head>
  <body>
    <div class="card">
      <h1>Authenticated</h1>
      <p>Welcome, <strong>${req.session.user.username}</strong></p>
      <p>Discord ID: ${req.session.user.id}</p>
      ${req.session.user.avatar
        ? `<img src="https://cdn.discordapp.com/avatars/${req.session.user.id}/${req.session.user.avatar}.png" alt="avatar" />`
        : ''}
      <p><a href="/logout">Logout</a></p>
    </div>
  </body>
</html>`);
  });

  app.get('/login', (req, res) => {
    const clientId = requireEnv('CLIENT_ID');
    const redirectUri = requireEnv('REDIRECT_URI');

    const params = new URLSearchParams({
      client_id: clientId,
      redirect_uri: redirectUri,
      response_type: 'code',
      scope: 'identify'
    });

    res.redirect(`https://discord.com/oauth2/authorize?${params.toString()}`);
  });

  app.get('/callback', async (req, res) => {
    const code = req.query.code;

    if (!code) {
      res.status(400).send('Missing OAuth2 code.');
      return;
    }

    try {
      const clientId = requireEnv('CLIENT_ID');
      const clientSecret = requireEnv('CLIENT_SECRET');
      const redirectUri = requireEnv('REDIRECT_URI');

      const tokenRes = await fetch(`${DISCORD_API_BASE}/oauth2/token`, {
        method: 'POST',
        headers: {
          'Content-Type': 'application/x-www-form-urlencoded'
        },
        body: new URLSearchParams({
          client_id: clientId,
          client_secret: clientSecret,
          grant_type: 'authorization_code',
          code: String(code),
          redirect_uri: redirectUri
        }).toString()
      });

      if (!tokenRes.ok) {
        const errText = await tokenRes.text();
        res.status(401).send(`Failed to get access token: ${errText}`);
        return;
      }

      const tokenData = await tokenRes.json();
      const userRes = await fetch(`${DISCORD_API_BASE}/users/@me`, {
        headers: {
          Authorization: `Bearer ${tokenData.access_token}`
        }
      });

      if (!userRes.ok) {
        const errText = await userRes.text();
        res.status(401).send(`Failed to fetch user profile: ${errText}`);
        return;
      }

      const user = await userRes.json();
      req.session.user = {
        id: user.id,
        username: user.username,
        avatar: user.avatar
      };

      res.redirect('/');
    } catch (error) {
      res.status(500).send(`OAuth2 callback error: ${error.message}`);
    }
  });

  app.get('/logout', (req, res) => {
    req.session.destroy(() => {
      res.redirect('/');
    });
  });

  app.get('/me', (req, res) => {
    if (!req.session.user) {
      res.status(401).json({ authenticated: false });
      return;
    }

    res.json({ authenticated: true, user: req.session.user });
  });

  return app;
}

function startServer(port = PORT) {
  const app = createServer();
  return app.listen(port, () => {
    console.log(`Server running at http://localhost:${port}`);
  });
}

if (require.main === module) {
  startServer();
}

module.exports = {
  createServer,
  startServer
};

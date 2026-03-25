const { app, BrowserWindow, dialog } = require('electron');
const { startServer } = require('./server');

let mainWindow;
let server;

function createWindow() {
  mainWindow = new BrowserWindow({
    width: 1100,
    height: 760,
    show: false,
    webPreferences: {
      contextIsolation: true,
      nodeIntegration: false
    }
  });

  mainWindow.once('ready-to-show', () => {
    mainWindow.show();
  });

  return mainWindow;
}

async function launchApp() {
  const win = createWindow();

  try {
    if (!process.env.PORT) {
      process.env.PORT = '3000';
    }

    server = startServer();
    await win.loadURL(`http://localhost:${process.env.PORT}`);
  } catch (error) {
    const message = error instanceof Error ? error.message : String(error);

    await win.loadURL(`data:text/html,${encodeURIComponent(`
      <html>
        <body style="font-family: Arial, sans-serif; background:#111; color:#fff; padding:24px;">
          <h2>App failed to start</h2>
          <p>${message}</p>
          <p>Check your .env file and ensure CLIENT_ID, CLIENT_SECRET, REDIRECT_URI, and SESSION_SECRET are set.</p>
        </body>
      </html>
    `)}`);

    dialog.showErrorBox('Startup Error', message);
  }
}

app.whenReady().then(async () => {
  await launchApp();

  app.on('activate', async () => {
    if (BrowserWindow.getAllWindows().length === 0) {
      await launchApp();
    }
  });
});

app.on('window-all-closed', () => {
  if (process.platform !== 'darwin') {
    app.quit();
  }
});

app.on('before-quit', () => {
  if (server) {
    server.close();
  }
});

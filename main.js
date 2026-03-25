const { app, BrowserWindow } = require('electron');
const { startServer } = require('./server');

let mainWindow;
let server;

function createWindow() {
  mainWindow = new BrowserWindow({
    width: 1100,
    height: 760,
    webPreferences: {
      contextIsolation: true,
      nodeIntegration: false
    }
  });

  return mainWindow.loadURL(`http://localhost:${process.env.PORT || 3000}`);
}

app.whenReady().then(async () => {
  if (!process.env.PORT) {
    process.env.PORT = '3000';
  }

  server = startServer();
  await createWindow();

  app.on('activate', async () => {
    if (BrowserWindow.getAllWindows().length === 0) {
      await createWindow();
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

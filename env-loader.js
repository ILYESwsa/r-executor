const fs = require('fs');
const path = require('path');
const dotenv = require('dotenv');

function unique(items) {
  return [...new Set(items.filter(Boolean))];
}

function loadEnv() {
  const candidates = unique([
    process.env.DOTENV_PATH,
    path.resolve(process.cwd(), '.env'),
    path.resolve(__dirname, '.env'),
    process.execPath ? path.resolve(path.dirname(process.execPath), '.env') : null
  ]);

  for (const filePath of candidates) {
    if (fs.existsSync(filePath)) {
      dotenv.config({ path: filePath });
      return filePath;
    }
  }

  dotenv.config();
  return null;
}

module.exports = {
  loadEnv
};

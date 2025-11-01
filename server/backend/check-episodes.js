const sqlite3 = require('sqlite3').verbose();
const path = require('path');

const dbPath = path.join(__dirname, 'data', 'lanflix.db');
const db = new sqlite3.Database(dbPath);

// Check series_episodes table
db.all("SELECT * FROM series_episodes WHERE content_id = 2 LIMIT 10", [], (err, rows) => {
  if (err) {
    console.error('Error querying episodes:', err);
    db.close();
    return;
  }
  console.log('Episodes:', JSON.stringify(rows, null, 2));
  db.close();
});

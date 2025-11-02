const sqlite3 = require('sqlite3').verbose();
const path = require('path');

const dbPath = path.join(__dirname, 'data', 'lanflix.db');
const db = new sqlite3.Database(dbPath);

// First check all tables
db.all("SELECT name FROM sqlite_master WHERE type='table'", [], (err, tables) => {
  if (err) {
    console.error('Error getting tables:', err);
    db.close();
    return;
  }
  console.log('Tables:', tables);
  
  // Get schema for content table
  db.all("PRAGMA table_info(content)", [], (err, schema) => {
    if (err) {
      console.error('Error getting schema:', err);
      db.close();
      return;
    }
    console.log('\nContent table schema:', JSON.stringify(schema, null, 2));
    
    // Query all content
    db.all("SELECT * FROM content LIMIT 5", [], (err, rows) => {
      if (err) {
        console.error('Error querying content:', err);
        db.close();
        return;
      }
      console.log('\nContent rows:', JSON.stringify(rows, null, 2));
      db.close();
    });
  });
});

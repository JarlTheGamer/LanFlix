const sqlite3 = require('sqlite3').verbose();
const path = require('path');

const dbPath = path.join(__dirname, 'data', 'lanflix.db');
const db = new sqlite3.Database(dbPath);

db.all("SELECT id, tmdb_id, title FROM content WHERE title LIKE '%Hazbin%'", [], (err, rows) => {
  if (err) {
    console.error('Error:', err);
    db.close();
    return;
  }
  console.log('Hazbin Hotel content:', JSON.stringify(rows, null, 2));
  
  if (rows.length > 0) {
    const contentId = rows[0].id;
    db.all(`SELECT * FROM series_episodes WHERE content_id = ${contentId} AND season_number = 2 LIMIT 5`, [], (err, episodes) => {
      if (err) {
        console.error('Error:', err);
        db.close();
        return;
      }
      console.log('\nSeason 2 episodes:', JSON.stringify(episodes, null, 2));
      db.close();
    });
  } else {
    db.close();
  }
});

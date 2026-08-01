import { useQuery } from '@tanstack/react-query';
import { getHome, MediaItem, resolveArtwork } from './api';

function Poster({ item }: { item: MediaItem }) {
  return (
    <button className="poster" type="button">
      <span className="poster-art" style={{ backgroundImage: `url(${resolveArtwork(item.posterUrl)})` }} />
      <strong>{item.title}</strong>
      <small>{item.year ?? item.type}</small>
    </button>
  );
}

function Shelf({ title, items }: { title: string; items: MediaItem[] }) {
  if (!items.length) return null;
  return <section className="shelf"><h2>{title}</h2><div className="rail">{items.map(item => <Poster key={`${item.type}-${item.id}`} item={item} />)}</div></section>;
}

export function App() {
  const home = useQuery({ queryKey: ['home'], queryFn: getHome });
  const hero = home.data?.hero ?? home.data?.recentlyAdded[0];

  return (
    <main>
      <header className="chrome">
        <a className="brand" href="/">lanflix</a>
        <nav><button aria-label="Search">⌕</button><button aria-label="Cast">▣</button><button aria-label="Watchlist">▮</button><button className="avatar" aria-label="Profile">●</button></nav>
      </header>
      <section className="hero" style={{ backgroundImage: `url(${resolveArtwork(hero?.backdropUrl ?? hero?.posterUrl)})` }}>
        <div className="hero-shade" />
        <div className="hero-copy">
          <p className="eyebrow">NOW ON YOUR SERVER</p>
          <h1>{hero?.title ?? (home.isLoading ? 'Loading your cinema…' : 'Welcome to Lanflix')}</h1>
          <p>{hero?.overview ?? home.error?.message ?? 'Your private media, beautifully organized.'}</p>
          {hero && <button className="play">▶ Play</button>}
        </div>
      </section>
      <div className="content">
        <Shelf title="Continue Watching" items={home.data?.continueWatching ?? []} />
        <Shelf title="Recently Added" items={home.data?.recentlyAdded ?? []} />
      </div>
    </main>
  );
}

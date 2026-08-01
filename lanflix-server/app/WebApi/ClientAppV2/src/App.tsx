import { CSSProperties, ReactNode, useState } from 'react';
import { useQuery } from '@tanstack/react-query';
import { BrowserRouter, NavLink, Route, Routes } from 'react-router-dom';
import {
  Bell, Cast, ChevronRight, Compass, Download, Film, Home as HomeIcon, Library,
  ListMusic, LogOut, Music2, Play, Search, Server, Settings, ShieldCheck, Tv, UserRound
} from 'lucide-react';
import { getHome, getLibrary, MediaItem, resolveArtwork } from './api';

const fallbackPalette = { base: '#143d5a', depth: '#081a2b', glow: '#186d92', accent: '#f5a623', onBackground: '#fff', algorithmVersion: 1 };

function Poster({ item }: { item: MediaItem }) {
  return (
    <button className="poster" type="button">
      <span className="poster-art" style={{ backgroundImage: `url(${resolveArtwork(item.posterUrl)})` }}>
        {item.progressPercentage ? <i style={{ width: `${item.progressPercentage}%` }} /> : null}
      </span>
      <strong>{item.title}</strong>
      <small>{item.year ?? item.type}{item.serverAvailable ? '' : ' · metadata only'}</small>
    </button>
  );
}

function Shelf({ title, items }: { title: string; items: MediaItem[] }) {
  if (!items.length) return null;
  return <section className="shelf"><div className="section-title"><h2>{title}</h2><ChevronRight size={18} /></div><div className="rail">{items.map(item => <Poster key={`${item.type}-${item.id}`} item={item} />)}</div></section>;
}

function AccountPill() {
  const [open, setOpen] = useState(false);
  return <div className="account-anchor">
    <button className="avatar" aria-label="Account menu" aria-expanded={open} onClick={() => setOpen(value => !value)}><UserRound size={18} /></button>
    {open ? <div className="account-pill" aria-label="Account actions">
      <NavLink to="/account" aria-label="Account"><UserRound /></NavLink>
      <NavLink to="/downloads" aria-label="Downloads"><Download /></NavLink>
      <NavLink to="/settings/server" aria-label="Server connection"><Server /></NavLink>
      <span />
      <NavLink to="/settings" aria-label="Settings"><Settings /></NavLink>
      <button aria-label="Sign out"><LogOut /></button>
    </div> : null}
  </div>;
}

const destinations = [
  ['/', 'Home', HomeIcon], ['/library', 'Libraries', Library], ['/live-tv', 'Live TV', Tv],
  ['/downloads', 'On Demand', Download], ['/discover', 'Discover', Compass]
] as const;

function Chrome() {
  return <>
    <header className="chrome">
      <NavLink className="brand" to="/">lanflix</NavLink>
      <nav className="desktop-nav">{destinations.map(([to, label]) => <NavLink key={to} to={to} end={to === '/'}>{label}</NavLink>)}</nav>
      <nav className="actions"><button aria-label="Search"><Search /></button><button aria-label="Cast"><Cast /></button><button aria-label="Notifications"><Bell /></button><AccountPill /></nav>
    </header>
    <nav className="mobile-nav">{destinations.map(([to, label, Icon]) => <NavLink key={to} to={to} end={to === '/'}><Icon /><span>{label}</span></NavLink>)}</nav>
  </>;
}

function HomePage() {
  const home = useQuery({ queryKey: ['home'], queryFn: getHome });
  const hero = home.data?.hero ?? home.data?.recentlyAdded[0];
  const palette = hero?.palette ?? fallbackPalette;
  const style = {
    '--base': palette.base, '--depth': palette.depth, '--glow': palette.glow,
    '--accent': palette.accent, '--on-bg': palette.onBackground
  } as CSSProperties;

  return <main className="cinematic" style={style}>
    <section className="hero" style={{ backgroundImage: `url(${resolveArtwork(hero?.backdropUrl ?? hero?.posterUrl)})` }}>
      <div className="hero-layers" />
      <div className="hero-copy">
        {hero?.logoUrl ? <img className="title-art" src={resolveArtwork(hero.logoUrl)} alt={hero.title} /> : null}
        <p className="metadata">{[hero?.year, hero?.rating, hero?.type].filter(Boolean).join('  ·  ')}</p>
        <p className="overview">{hero?.overview ?? (home.isLoading ? 'Loading your cinema…' : 'Your private media, beautifully organized.')}</p>
        {hero ? <button className="play"><Play fill="currentColor" />{hero.progressPercentage ? 'Resume' : 'Play'}</button> : null}
      </div>
    </section>
    <div className="content">
      {home.isError ? <StateCard title="Server unavailable" copy="Lanflix could not refresh this page. Your downloaded media remains available in On Demand." /> : null}
      <Shelf title="Continue Watching" items={home.data?.continueWatching ?? []} />
      <Shelf title="Recently Added" items={home.data?.recentlyAdded ?? []} />
      <Promo icon={Music2} title="Your music, reimagined" copy="Albums, mixes, radios and offline listening." to="/music" />
    </div>
  </main>;
}

function LibraryPage() {
  const library = useQuery({ queryKey: ['library'], queryFn: () => getLibrary() });
  return <Page title="Libraries" subtitle="Movies, series, music and collections from your server.">
    <div className="filter-row"><button className="selected">All</button><button>Movies</button><button>Series</button><button>Music</button><button>Collections</button></div>
    <div className="poster-grid">{library.data?.items.map(item => <Poster key={`${item.type}-${item.id}`} item={item} />)}</div>
    {library.isLoading ? <StateCard title="Loading your library" copy="Reading cached metadata and server availability…" /> : null}
  </Page>;
}

function Page({ title, subtitle, children }: { title: string; subtitle: string; children?: ReactNode }) {
  return <main className="page"><div className="page-glow" /><header><h1>{title}</h1><p>{subtitle}</p></header>{children}</main>;
}

function StateCard({ title, copy }: { title: string; copy: string }) { return <div className="state-card"><strong>{title}</strong><p>{copy}</p></div>; }
function Promo({ icon: Icon, title, copy, to }: { icon: typeof Music2; title: string; copy: string; to: string }) { return <NavLink className="promo" to={to}><Icon /><span><strong>{title}</strong><small>{copy}</small></span><ChevronRight /></NavLink>; }

function FeaturePage({ kind }: { kind: 'music' | 'live' | 'downloads' | 'discover' }) {
  const config = {
    music: [Music2, 'Music', 'Albums, artists, playlists, smart mixes, lyrics and your persistent queue.'],
    live: [Tv, 'Live TV', 'Your channel guide, favorites and now/next programs.'],
    downloads: [Download, 'On Demand', 'Device downloads, server requests, queue progress and storage.'],
    discover: [Compass, 'Discover', 'Trending media, people, requests and server availability.']
  }[kind] as [typeof Music2, string, string];
  const Icon = config[0];
  return <Page title={config[1]} subtitle={config[2]}><StateCard title="Module ready for data" copy="The unified route and design shell are active; this view fills as its v2 module endpoints are enabled." /><div className="feature-icon"><Icon /></div></Page>;
}

function SettingsPage() {
  return <Page title="Settings" subtitle="Account, server, playback, downloads, appearance, privacy and devices.">
    <div className="settings-grid">
      <Promo icon={UserRound} title="Account & security" copy="Password, passkeys, sessions and permissions" to="/account" />
      <Promo icon={Server} title="Server connection" copy="Current, discovered and saved servers" to="/settings/server" />
      <Promo icon={Film} title="Playback" copy="Quality, audio, subtitles and autoplay" to="/settings/playback" />
      <Promo icon={ShieldCheck} title="Administration" copy="Health, libraries, accounts, integrations and logs" to="/admin" />
    </div>
  </Page>;
}

function AdminPage() { return <Page title="Administration" subtitle="Role-gated server operations."><div className="admin-grid">{['Server health', 'Libraries & scans', 'Accounts & invitations', 'Transcoding', 'Music analysis', 'Live TV sources', 'Updates', 'Audit history'].map(label => <button key={label}>{label}<ChevronRight /></button>)}</div></Page>; }

export function App() {
  return <BrowserRouter><Chrome /><Routes>
    <Route path="/" element={<HomePage />} />
    <Route path="/library" element={<LibraryPage />} />
    <Route path="/music" element={<FeaturePage kind="music" />} />
    <Route path="/live-tv" element={<FeaturePage kind="live" />} />
    <Route path="/downloads" element={<FeaturePage kind="downloads" />} />
    <Route path="/discover" element={<FeaturePage kind="discover" />} />
    <Route path="/settings/*" element={<SettingsPage />} />
    <Route path="/account" element={<Page title="Account" subtitle="Security, sessions and server permissions." />} />
    <Route path="/admin/*" element={<AdminPage />} />
    <Route path="*" element={<Page title="Not found" subtitle="This Lanflix route does not exist." />} />
  </Routes></BrowserRouter>;
}

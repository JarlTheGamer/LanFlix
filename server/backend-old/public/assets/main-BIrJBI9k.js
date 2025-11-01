import{P as x}from"./profile-manager-BH47FVgT.js";import{s as p}from"./data-DddsZ_ae.js";import{a as u}from"./api-client-7IhkegDa.js";import{t as C}from"./tv-navigation-BRt9J30j.js";class y{constructor(e){this.profileManager=e,this.modal=null,this.currentContent=null}async show(e,t,s=!1){try{const o=this.profileManager.selectedProfileId,i=s?await u.getContentDetails(e,t,o):await u.getLibraryItem(e,o);if(t==="series"&&s)try{const n=await u.getSeriesEpisodes(e);i.seasons=n.seasons,i.numberOfSeasons=n.numberOfSeasons,i.numberOfEpisodes=n.numberOfEpisodes,i.episodes=[],i.tmdbId=e}catch(n){console.error("Failed to fetch season metadata:",n),i.seasons=[],i.episodes=[]}this.currentContent=i,this.createModal(i,s),requestAnimationFrame(()=>{this.modal.classList.add("visible")}),this.setupCloseHandlers()}catch(o){console.error("Failed to load content details:",o),alert("Failed to load content details.")}}createModal(e,t){this.close();const s=document.createElement("div");s.className="content-modal",s.id="content-modal";const o=e.backdropUrl||e.posterUrl||"",i=e.posterUrl||"",n=Array.isArray(e.genres)?e.genres.join(", "):"",l=e.releaseDate?new Date(e.releaseDate).getFullYear():"",a=e.voteAverage?`★ ${e.voteAverage.toFixed(1)}`:"";let r="";if(e.runtime&&e.runtime>0){const f=Math.floor(e.runtime/60),m=e.runtime%60;f>0?r=`${f}h ${m}m`:r=`${m}m`}const c=e.episodes||[],d=e.numberOfEpisodes||c.length,h=e.type==="series"&&(e.seasons?.length>0||c.length>0);s.innerHTML=`
      <div class="modal-ambilight"></div>
      <div class="modal-overlay"></div>
      
      <div class="modal-content">
        <button class="modal-close" aria-label="Close">
          <svg viewBox="0 0 24 24"><path d="M19 6.41L17.59 5 12 10.59 6.41 5 5 6.41 10.59 12 5 17.59 6.41 19 12 13.41 17.59 19 19 17.59 13.41 12z"/></svg>
        </button>

        <div class="modal-header">
          <div class="modal-poster">
            <img src="${i}" alt="${e.title}" />
          </div>
          <div class="modal-info">
            <h1 class="modal-title">${e.title}</h1>
            <div class="modal-meta">
              ${l?`<span>${l}</span>`:""}
              ${a?`<span>${a}</span>`:""}
              ${r?`<span>${r}</span>`:""}
              ${e.type==="series"&&d>0?`<span>${d} Episodes</span>`:""}
            </div>
            <div class="modal-genres">${n}</div>
            <p class="modal-description">${e.overview||"No description available."}</p>
            
            <div class="modal-actions">
              ${t?`
                <button class="modal-btn primary" data-action="queue-all">
                  <svg viewBox="0 0 24 24"><path d="M19 13h-6v6h-2v-6H5v-2h6V5h2v6h6v2z"/></svg>
                  Download ${e.type==="series"?"All Episodes":"Movie"}
                </button>
              `:`
                <button class="modal-btn primary" data-action="play">
                  <svg viewBox="0 0 24 24"><path d="M8 5v14l11-7z"/></svg>
                  Play
                </button>
              `}
              <button class="modal-btn secondary" data-action="watchlist">
                <svg viewBox="0 0 24 24"><path d="M19 13h-6v6h-2v-6H5v-2h6V5h2v6h6v2z"/></svg>
                My List
              </button>
            </div>
          </div>
        </div>

        ${h?`
          <div class="modal-episodes">
            <h2>Episodes</h2>
            <div class="episodes-list" id="episodes-list"></div>
          </div>
        `:""}
      </div>
    `,document.body.appendChild(s),this.modal=s,h&&this.renderEpisodes(c,t),this.setupActionHandlers(e,t),this.applyAmbilightEffect(o)}renderEpisodes(e,t){const s=document.getElementById("episodes-list");if(!s)return;const o=this.currentContent.seasons||[],i=o.length>0;let n;if(i)n=o.map(a=>a.seasonNumber.toString()).sort((a,r)=>parseInt(a)-parseInt(r));else{const a={};e.forEach(r=>{a[r.seasonNumber]||(a[r.seasonNumber]=[]),a[r.seasonNumber].push(r)}),n=Object.keys(a).sort((r,c)=>parseInt(r)-parseInt(c))}s.innerHTML=`
      <div class="episodes-layout">
        <div class="seasons-sidebar">
          ${n.map((a,r)=>{const c=i?o.find(h=>h.seasonNumber.toString()===a):null,d=c?c.episodeCount:e.filter(h=>h.seasonNumber.toString()===a).length;return`
            <button class="season-tab ${r===0?"active":""}" data-season="${a}">
              <div class="season-tab-title">Season ${a}</div>
              <div class="season-tab-count">${d} episodes</div>
            </button>
          `}).join("")}
        </div>
        <div class="episodes-content">
          ${n.map((a,r)=>`
            <div class="season-episodes ${r===0?"active":""}" data-season="${a}">
              <div class="season-header">
                <h3>Season ${a}</h3>
                <button class="season-download-btn" data-season="${a}" style="display: none;">
                  <svg viewBox="0 0 24 24"><path d="M19 13h-6v6h-2v-6H5v-2h6V5h2v6h6v2z"/></svg>
                  <span class="download-btn-text">${t?"Download Season":"Download Missing"}</span>
                </button>
              </div>
              <div class="episodes-list-vertical" data-season="${a}">
                ${r===0?"":'<div class="loading-placeholder">Loading episodes...</div>'}
              </div>
            </div>
          `).join("")}
        </div>
      </div>
    `,n.length>0&&this.loadAllSeasonsProgressively(n,t);const l=s.querySelectorAll(".season-tab");l.forEach(a=>{a.addEventListener("click",async()=>{const r=a.dataset.season;l.forEach(d=>d.classList.remove("active")),a.classList.add("active"),s.querySelectorAll(".season-episodes").forEach(d=>{d.classList.remove("active"),d.dataset.season===r&&d.classList.add("active")}),await this.loadSeasonEpisodes(r,t)})})}async loadAllSeasonsProgressively(e,t){if(!t){for(const s of e)try{await this.loadSeasonEpisodes(s,t)}catch(o){console.error(`Failed to load season ${s}:`,o)}return}for(const s of e)try{await this.loadSeasonEpisodes(s,t),await new Promise(o=>setTimeout(o,100))}catch(o){console.error(`Failed to load season ${s}:`,o)}}async loadSeasonEpisodes(e,t){const s=document.querySelector(`.episodes-list-vertical[data-season="${e}"]`);if(s&&!s.querySelector(".episode-card-horizontal")){s.innerHTML='<div class="loading-placeholder">Loading episodes...</div>';try{let o=[];if(t&&this.currentContent.tmdbId)o=(await u.getSeasonEpisodes(this.currentContent.tmdbId,parseInt(e))).season.episodes;else{const l=parseInt(e);o=(this.currentContent.episodes||[]).filter(a=>parseInt(a.seasonNumber)===l)}s.innerHTML="",o.forEach(l=>{const a=this.createEpisodeCard(l,t);s.appendChild(a)});const i=!t&&o.some(l=>!l.available),n=document.querySelector(`.season-download-btn[data-season="${e}"]`);n&&(t||i?(n.style.display="flex",n.addEventListener("click",()=>{this.downloadSeason(e)})):n.style.display="none")}catch(o){console.error(`Failed to load season ${e}:`,o),s.innerHTML='<div class="error-placeholder">Failed to load episodes</div>'}}}createEpisodeCard(e,t){const s=document.createElement("div"),o=e.available!==!1,i=!t;s.className=`episode-card-horizontal ${!o&&i?"unavailable":""}`,s.dataset.episodeId=e.id,s.dataset.seasonNumber=e.seasonNumber,s.dataset.episodeNumber=e.episodeNumber;const n=e.stillPath||this.currentContent.backdropUrl||"",l=e.watched||!1,a=e.runtime?`${e.runtime}m`:"";return s.innerHTML=`
          <div class="episode-thumbnail-horizontal">
            <img src="${n}" alt="Episode ${e.episodeNumber}" />
            ${l?'<div class="watched-badge">✓</div>':""}
            ${!o&&i?'<div class="unavailable-badge">Not Downloaded</div>':""}
            ${o&&i?`
              <button class="episode-play-btn">
                <svg viewBox="0 0 24 24"><path d="M8 5v14l11-7z"/></svg>
              </button>
            `:""}
          </div>
          <div class="episode-info-horizontal">
            <div class="episode-header-row">
              <div class="episode-number-title">
                <span class="episode-number">${e.episodeNumber}.</span>
                <span class="episode-title">${e.title||`Episode ${e.episodeNumber}`}</span>
              </div>
              ${a?`<span class="episode-runtime">${a}</span>`:""}
            </div>
            <div class="episode-overview">${e.overview||"No description available."}</div>
            ${t||!o&&i?`
              <button class="episode-download-btn" data-episode-id="${e.id}">
                <svg viewBox="0 0 24 24"><path d="M19 13h-6v6h-2v-6H5v-2h6V5h2v6h6v2z"/></svg>
                Download Episode
              </button>
            `:""}
          </div>
        `,o&&i&&s.querySelector(".episode-play-btn")?.addEventListener("click",c=>{c.stopPropagation(),this.playEpisode(e.id)}),(t||!o&&i)&&s.querySelector(".episode-download-btn")?.addEventListener("click",c=>{c.stopPropagation(),this.downloadEpisode(e.seasonNumber,e.episodeNumber)}),s}setupActionHandlers(e,t){const s=this.modal;s.querySelector('[data-action="play"]')?.addEventListener("click",()=>{if(e.type==="series"){const i=(e.episodes||[]).find(n=>n.available);i?this.playEpisode(i.id):alert("No episodes available to play. Please download episodes first.")}else window.location.href=`player.html?contentId=${e.id}&type=${e.type}`}),s.querySelector('[data-action="queue-all"]')?.addEventListener("click",async()=>{await this.queueDownload(e)}),s.querySelector('[data-action="watchlist"]')?.addEventListener("click",async()=>{await this.toggleWatchlist(e.id)})}async queueDownload(e){try{const t=this.profileManager.selectedProfileId;if(!t){alert("Please select a profile first");return}const s=this.modal.querySelector('[data-action="queue-all"]');s&&(s.disabled=!0,s.innerHTML='<svg viewBox="0 0 24 24"><path d="M12 2C6.48 2 2 6.48 2 12s4.48 10 10 10 10-4.48 10-10S17.52 2 12 2zm-2 15l-5-5 1.41-1.41L10 14.17l7.59-7.59L19 8l-9 9z"/></svg> Added to Queue'),await u.queueDownload(e.tmdbId||e.id,t,e.type,e.title,e.releaseDate?new Date(e.releaseDate).getFullYear():null),setTimeout(()=>{s&&(s.innerHTML='<svg viewBox="0 0 24 24"><path d="M19 13h-6v6h-2v-6H5v-2h6V5h2v6h6v2z"/></svg> Download '+(e.type==="series"?"All Episodes":"Movie"),s.disabled=!1)},2e3)}catch(t){console.error("Failed to queue download:",t),alert("Failed to add to download queue.")}}async downloadEpisode(e,t){try{const s=this.profileManager.selectedProfileId;if(!s){alert("Please select a profile first");return}const o=this.currentContent;await u.queueEpisodeDownload(o.tmdbId||o.id,s,o.title,e,t,o.releaseDate?new Date(o.releaseDate).getFullYear():null),alert(`"${o.title}" S${e}E${t} has been added to your download queue!`)}catch(s){console.error("Failed to queue episode download:",s),alert("Failed to add episode to download queue.")}}async downloadSeason(e){try{const t=this.profileManager.selectedProfileId;if(!t){alert("Please select a profile first");return}const s=this.currentContent;await u.queueSeasonDownload(s.tmdbId||s.id,t,s.title,e,s.releaseDate?new Date(s.releaseDate).getFullYear():null),alert(`"${s.title}" Season ${e} has been added to your download queue!`)}catch(t){console.error("Failed to queue season download:",t),alert("Failed to add season to download queue.")}}playEpisode(e){const t=this.currentContent.id;window.location.href=`player.html?contentId=${t}&type=series&episodeId=${e}`}async toggleWatchlist(e){try{const t=this.profileManager.selectedProfileId;if(!t){alert("Please select a profile first");return}(await u.getWatchlist(t)).items?.some(i=>i.contentId===e)?(await u.removeFromWatchlist(t,e),alert("Removed from My List")):(await u.addToWatchlist(t,e),alert("Added to My List"))}catch(t){console.error("Failed to toggle watchlist:",t),alert("Failed to update My List.")}}applyAmbilightEffect(e){const t=this.modal.querySelector(".modal-ambilight");t&&e&&(t.style.backgroundImage=`url('${e}')`)}setupCloseHandlers(){const e=this.modal.querySelector(".modal-close"),t=this.modal.querySelector(".modal-overlay");e?.addEventListener("click",()=>this.close()),t?.addEventListener("click",()=>this.close());const s=o=>{o.key==="Escape"&&(this.close(),document.removeEventListener("keydown",s))};document.addEventListener("keydown",s)}close(){this.modal&&(this.modal.classList.remove("visible"),setTimeout(()=>{this.modal.remove(),this.modal=null,this.currentContent=null},300))}}const E=Object.freeze(Object.defineProperty({__proto__:null,ContentModal:y,default:y},Symbol.toStringTag,{value:"Module"}));class S{constructor(e){this.profileManager=e,this.contentModal=new y(e),this.currentCategory="home",this.currentHeroIndex=0,this.activeAmbilightLayer=1,this.focusedHeroElement=null,this.contentData={},this.isLoading=!1,this.imageObserver=null,this.root=document.documentElement,this.heroCarouselTrack=document.getElementById("hero-carousel-track"),this.heroAmbilight=document.getElementById("hero-ambilight"),this.ambilightLayer1=document.getElementById("ambilight-layer-1"),this.ambilightLayer2=document.getElementById("ambilight-layer-2"),this.topNav=document.querySelector(".top-nav")}async initialize(){this.currentCategory="home",document.querySelectorAll(".menu-item").forEach(s=>s.classList.remove("active"));const t=document.querySelector('.menu-item[data-hero="home"]');t&&t.classList.add("active"),await this.loadContent(),await this.renderHomePage(),this.setupScrollHandler(),this.setupOfflineHandlers()}setupOfflineHandlers(){window.addEventListener("api-offline",()=>{this.showOfflineNotification()}),window.addEventListener("api-online",()=>{this.hideOfflineNotification(),this.refreshContent()}),window.addEventListener("data-refresh-needed",()=>{this.refreshContent()})}showOfflineNotification(){this.hideOfflineNotification();const e=document.createElement("div");e.id="offline-notification",e.style.cssText=`
      position: fixed;
      top: 60px;
      left: 50%;
      transform: translateX(-50%);
      background: rgba(255, 152, 0, 0.95);
      color: #000;
      padding: 12px 24px;
      border-radius: 8px;
      font-size: 14px;
      font-weight: 500;
      z-index: 10000;
      box-shadow: 0 4px 12px rgba(0, 0, 0, 0.3);
      animation: slideDown 0.3s ease-out;
    `,e.innerHTML=`
      <span style="margin-right: 8px;">🔴</span>
      Discovery features are offline. Your downloaded content is still available.
      <span style="margin-left: 8px; font-size: 12px; opacity: 0.8;">Retrying in 10 minutes...</span>
    `,document.body.appendChild(e);const t=document.createElement("style");t.textContent=`
      @keyframes slideDown {
        from {
          opacity: 0;
          transform: translateX(-50%) translateY(-20px);
        }
        to {
          opacity: 1;
          transform: translateX(-50%) translateY(0);
        }
      }
    `,document.head.appendChild(t)}hideOfflineNotification(){const e=document.getElementById("offline-notification");e&&(e.style.animation="slideUp 0.3s ease-out",setTimeout(()=>e.remove(),300))}async loadContent(){if(!this.isLoading){this.isLoading=!0;try{const e=this.profileManager.selectedProfileId;switch(this.currentCategory){case"home":const t=await p.getRecentlyAdded(20);let s={trending:{movies:[],series:[]}};!u.isOffline&&!p.isOffline&&(s=await p.getDiscoverContent(e).catch(()=>({trending:{movies:[],series:[]}})));const o=[...s.trending?.movies||[],...s.trending?.series||[]].slice(0,10);this.contentData={recentlyAdded:t.items||[],discoverPreview:o};break;case"discover":if(u.isOffline||p.isOffline)this.contentData={trending:{movies:[],series:[]},popularMovies:[],popularSeries:[]};else try{const[a,r,c]=await Promise.all([p.getDiscoverContent(e),p.getPopularContent("movie",1,e),p.getPopularContent("series",1,e)]);this.contentData={trending:a.trending||{movies:[],series:[]},popularMovies:Array.isArray(r)?r:r?.items||[],popularSeries:Array.isArray(c)?c:c?.items||[]}}catch(a){console.error("Failed to load discovery content:",a),this.contentData={trending:{movies:[],series:[]},popularMovies:[],popularSeries:[]}}break;case"shows":const i=await p.getLibrarySeries({limit:100});this.contentData={series:i.items||[]};break;case"movies":const n=await p.getLibraryMovies({limit:100});this.contentData={movies:n.items||[]};break;case"my":const l=await p.getWatchlist(e);this.contentData={watchlist:l.items?.map(a=>a.content)||[]};break}}catch(e){console.error("Failed to load content:",e),this.contentData={}}finally{this.isLoading=!1}}}createCarouselItems(){if(this.heroCarouselTrack.innerHTML="",this.currentCategory==="discover"){const e=this.contentData.trending||{movies:[],series:[]},t=[...e.movies||[],...e.series||[]].slice(0,5);if(t.length===0){const s=this.createEmptyHero();this.heroCarouselTrack.appendChild(s)}else t.forEach((s,o)=>{const i=this.createDiscoveryHero(s,o);this.heroCarouselTrack.appendChild(i)})}else{const e=this.getLocalContentForHero();if(e.length===0){const t=this.createEmptyHero();this.heroCarouselTrack.appendChild(t)}else e.forEach((t,s)=>{const o=this.createHeroFromContent(t,s);this.heroCarouselTrack.appendChild(o)})}this.focusedHeroElement=this.heroCarouselTrack.querySelector(".hero"),this.focusedHeroElement&&this.focusedHeroElement.classList.add("focused")}getLocalContentForHero(){switch(this.currentCategory){case"home":return(this.contentData.recentlyAdded||[]).slice(0,5);case"movies":return(this.contentData.movies||[]).slice(0,5);case"shows":return(this.contentData.series||[]).slice(0,5);case"my":return(this.contentData.watchlist||[]).slice(0,5);default:return[]}}createHeroFromContent(e,t){const s=document.createElement("section");s.className="hero",s.dataset.index=t,s.dataset.contentId=e.id,s.dataset.contentType=e.type;const o=e.backdropUrl||e.posterUrl||'data:image/svg+xml,%3Csvg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 1920 1080"%3E%3Crect fill="%23222" width="1920" height="1080"/%3E%3Ctext x="50%25" y="50%25" fill="%23666" font-size="48" text-anchor="middle" dominant-baseline="middle"%3ENo Image%3C/text%3E%3C/svg%3E',i=Array.isArray(e.genres)?e.genres.join(" • "):e.genre||"Unknown",n=e.releaseDate?new Date(e.releaseDate).getFullYear():e.year||"",l=e.runtime?`${Math.floor(e.runtime/60)}h ${e.runtime%60}m`:e.duration||"",a=e.contentRating||e.rating||"NR",c=[e.type==="movie"?"Movie":"Series",n,a,l].filter(Boolean);s.innerHTML=`
      <div class="hero-background" style="background-image: url('${o}')"></div>
      <div class="hero-overlay"></div>
      <div class="hero-body">
        <div class="hero-content">
          <div class="hero-tag">Your Library • ${i}</div>
          <h1 class="hero-title">${e.title}</h1>
          <div class="hero-meta">${c.map(f=>`<span>${f}</span>`).join("")}</div>
          <p class="hero-description">${e.overview||e.description||"No description available."}</p>
          <div class="hero-actions">
            <button class="cta primary" data-action="play">
              <span>▶ Play</span>
            </button>
            <button class="cta ghost" data-action="info">
              <span>More Info</span>
            </button>
          </div>
        </div>
        <div class="hero-secondary"><span>Downloaded</span> Ready to watch</div>
      </div>
    `;const d=s.querySelector('[data-action="play"]'),h=s.querySelector('[data-action="info"]');return d&&d.addEventListener("click",()=>{window.location.href=`player.html?contentId=${e.id}&type=${e.type}`}),h&&h.addEventListener("click",()=>{this.handleInfoAction(e.id,e.type)}),s}createDiscoveryHero(e,t){const s=document.createElement("section");s.className="hero",s.dataset.index=t,s.dataset.contentId=e.tmdbId||e.id,s.dataset.contentType=e.type;const o=e.backdropUrl||e.posterUrl||'data:image/svg+xml,%3Csvg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 1920 1080"%3E%3Crect fill="%23222" width="1920" height="1080"/%3E%3Ctext x="50%25" y="50%25" fill="%23666" font-size="48" text-anchor="middle" dominant-baseline="middle"%3ENo Image%3C/text%3E%3C/svg%3E',i=Array.isArray(e.genres)&&e.genres.length>0?e.genres.join(" • "):"Trending",n=e.releaseDate?new Date(e.releaseDate).getFullYear():"",l=e.voteAverage?`★ ${e.voteAverage.toFixed(1)}`:"",r=[e.type==="movie"?"Movie":"Series",n,l].filter(Boolean);s.innerHTML=`
      <div class="hero-background" style="background-image: url('${o}')"></div>
      <div class="hero-overlay"></div>
      <div class="hero-body">
        <div class="hero-content">
          <div class="hero-tag">🔥 Trending • ${i}</div>
          <h1 class="hero-title">${e.title}</h1>
          <div class="hero-meta">${r.map(h=>`<span>${h}</span>`).join("")}</div>
          <p class="hero-description">${e.overview||"No description available."}</p>
          <div class="hero-actions">
            <button class="cta primary" data-action="queue">
              <span>+ Add to Queue</span>
            </button>
            <button class="cta ghost" data-action="info">
              <span>More Info</span>
            </button>
          </div>
        </div>
        <div class="hero-secondary"><span>Discover</span> Available to download</div>
      </div>
    `;const c=s.querySelector('[data-action="queue"]'),d=s.querySelector('[data-action="info"]');return c&&c.addEventListener("click",()=>{this.handleQueueAction(e.tmdbId||e.id,e.type)}),d&&d.addEventListener("click",()=>{this.handleInfoAction(e.tmdbId||e.id,e.type)}),s}createHeroFromMock(e,t){const s=document.createElement("section");return s.className="hero",s.dataset.index=t,s.innerHTML=`
      <div class="hero-background" style="background-image: ${e.background}"></div>
      <div class="hero-overlay"></div>
      <div class="hero-body">
        <div class="hero-content">
          <div class="hero-tag">${e.tag}</div>
          <h1 class="hero-title">${e.title}</h1>
          <div class="hero-meta">${e.meta.map(o=>`<span>${o}</span>`).join("")}</div>
          <p class="hero-description">${e.description}</p>
          <div class="hero-actions">
            <button class="cta primary">
              <span>Remind Me</span>
            </button>
            <button class="cta ghost">
              <span>More Info</span>
            </button>
          </div>
        </div>
        <div class="hero-secondary"><span>New</span> ${e.secondary}</div>
      </div>
    `,s}createEmptyHero(){const e=document.createElement("section");return e.className="hero",e.dataset.index=0,e.innerHTML=`
      <div class="hero-background" style="background: linear-gradient(135deg, #1a1a1a 0%, #2d2d2d 100%)"></div>
      <div class="hero-overlay"></div>
      <div class="hero-body">
        <div class="hero-content">
          <div class="hero-tag">Your Library</div>
          <h1 class="hero-title">No Content Yet</h1>
          <div class="hero-meta"><span>Empty Library</span></div>
          <p class="hero-description">Your library is empty. Go to Discovery to find and download content to watch!</p>
          <div class="hero-actions">
            <button class="cta primary" onclick="window.location.href='#'; document.querySelector('[data-hero=\\'discover\\']').click();">
              <span>Browse Discovery</span>
            </button>
          </div>
        </div>
        <div class="hero-secondary"><span>Tip</span> Download content to start watching</div>
      </div>
    `,e}updateCarouselPosition(){this.heroCarouselTrack.querySelectorAll(".hero").forEach((t,s)=>{const o=(s-this.currentHeroIndex)*100;t.style.transform=`translateX(${o}%)`,t.style.opacity=s===this.currentHeroIndex?"1":"0",t.style.scale=s===this.currentHeroIndex?"1":"0.9",t.style.zIndex=s===this.currentHeroIndex?"2":"0"})}goToSlide(e){const t=this.heroCarouselTrack.querySelectorAll(".hero").length;e<0?this.currentHeroIndex=t-1:e>=t?this.currentHeroIndex=0:this.currentHeroIndex=e,this.updateCarouselPosition(),this.updateAmbilightForCurrentSlide(),this.updateFocusedHero()}updateAmbilightForCurrentSlide(){const t=this.heroCarouselTrack.querySelectorAll(".hero")[this.currentHeroIndex];if(!t)return;const s=t.querySelector(".hero-background"),o=s?s.style.backgroundImage:"";this.root&&this.root.style.setProperty("--hero-bg-image",o),this.activeAmbilightLayer===1?(this.ambilightLayer2.style.backgroundImage=o,this.ambilightLayer2.offsetWidth,this.ambilightLayer2.classList.add("active"),this.ambilightLayer1.classList.remove("active"),this.activeAmbilightLayer=2):(this.ambilightLayer1.style.backgroundImage=o,this.ambilightLayer1.offsetWidth,this.ambilightLayer1.classList.add("active"),this.ambilightLayer2.classList.remove("active"),this.activeAmbilightLayer=1)}updateFocusedHero(){const e=this.heroCarouselTrack.querySelectorAll(".hero");e.forEach((t,s)=>{t.classList.toggle("focused",s===this.currentHeroIndex)}),this.focusedHeroElement=e[this.currentHeroIndex]}async switchCategory(e){if(this.currentCategory=e,this.currentHeroIndex=0,p.currentPage=e,p.saveState(),e==="discover"&&!await u.checkConnection()){console.log("Discovery page - API is offline, showing offline message"),this.showDiscoveryOfflinePage();return}switch(await this.loadContent(),e){case"home":await this.renderHomePage();break;case"discover":await this.renderDiscoverPage();break;case"movies":await this.renderMoviesPage();break;case"shows":await this.renderShowsPage();break;case"my":await this.renderMyListPage();break;default:await this.renderHomePage()}}async renderCards(e){const t=document.getElementById("spotlight-row");if(!t)return;if(t.innerHTML="",this.currentCategory==="discover"&&(p.isOffline||u.isOffline)){const l=document.createElement("div");l.className="movie-hub";const a=document.createElement("div");a.style.cssText="text-align: center; padding: 60px 20px; color: #999; width: 100%;",a.innerHTML=`
        <div style="font-size: 48px; margin-bottom: 20px;">📡</div>
        <h2 style="color: #fff; margin-bottom: 20px;">Discovery Features Offline</h2>
        <p style="font-size: 18px; margin-bottom: 10px;">Discovery features require an internet connection.</p>
        <p style="font-size: 16px; margin-bottom: 30px;">We'll automatically retry in 10 minutes.</p>
        <button id="retry-connection-btn" style="
          background: #e50914;
          color: white;
          border: none;
          padding: 12px 32px;
          font-size: 16px;
          border-radius: 4px;
          cursor: pointer;
          font-weight: 600;
          transition: background 0.2s;
        " onmouseover="this.style.background='#f40612'" onmouseout="this.style.background='#e50914'">
          Retry Now
        </button>
        <p style="font-size: 14px; margin-top: 40px; color: #666;">
          Your downloaded content is still available in Home, Movies, Series, and My List.
        </p>
      `,l.appendChild(a),t.appendChild(l),document.getElementById("retry-connection-btn")?.addEventListener("click",async()=>{const r=document.getElementById("retry-connection-btn");r&&(r.textContent="Checking...",r.disabled=!0),await u.checkConnection()?await this.refreshContent():r&&(r.textContent="Still Offline - Try Again",r.disabled=!1)});return}const s=document.createElement("div");s.className="movie-hub";let o=[],i=!1;switch(this.currentCategory){case"home":o=this.contentData.recentlyAdded||[],i=!0;break;case"discover":const l=this.contentData.trending?.movies||[],a=this.contentData.trending?.series||[],r=Array.isArray(this.contentData.popularMovies)?this.contentData.popularMovies:this.contentData.popularMovies?.items||[],c=Array.isArray(this.contentData.popularSeries)?this.contentData.popularSeries:this.contentData.popularSeries?.items||[],d=[...l,...a,...r,...c],h=new Set;o=d.filter(f=>{const m=f.tmdbId||f.id;return h.has(m)?!1:(h.add(m),!0)});break;case"shows":o=this.contentData.series||[];break;case"movies":o=this.contentData.movies||[];break;case"my":o=this.contentData.watchlist||[];break;default:o=[]}const n=e==="all"?o:o.filter(l=>l.type===e);if(i&&this.contentData.discoverPreview?.length>0&&!p.isOffline&&!u.isOffline){const l=document.createElement("div");l.className="discovery-carousel-section",l.innerHTML=`
        <h2 style="color: #fff; margin: 20px 0 10px 0; font-size: 24px;">Discover New Content</h2>
      `;const a=document.createElement("div");a.className="movie-hub",this.contentData.discoverPreview.forEach((c,d)=>{const h=this.createContentCard(c,d,!0);a.appendChild(h)}),l.appendChild(a),t.appendChild(l);const r=document.createElement("h2");r.style.cssText="color: #fff; margin: 40px 0 10px 0; font-size: 24px;",r.textContent="Your Library",t.appendChild(r)}if(n.length===0){const l=document.createElement("div");l.style.cssText="text-align: center; padding: 60px 20px; color: #999;",this.currentCategory==="home"?l.innerHTML=`
          <h2 style="color: #fff; margin-bottom: 20px;">Your Library is Empty</h2>
          <p style="font-size: 18px;">Go to Discovery to find and download content!</p>
        `:this.currentCategory==="my"?l.innerHTML=`
          <h2 style="color: #fff; margin-bottom: 20px;">Your List is Empty</h2>
          <p style="font-size: 18px;">Add content to your list to see it here.</p>
        `:l.innerHTML=`
          <h2 style="color: #fff; margin-bottom: 20px;">No Content Found</h2>
          <p style="font-size: 18px;">Download some content to see it here!</p>
        `,t.appendChild(l);return}n.forEach((l,a)=>{const r=this.createContentCard(l,a,this.currentCategory==="discover");s.appendChild(r)}),t.appendChild(s),this.setupLazyLoading(),this.setupCardHandlers()}createContentCard(e,t,s=!1){const o=document.createElement("article");o.className="movie-card",o.dataset.index=t,o.dataset.contentId=e.id||e.tmdbId,o.dataset.contentType=e.type;const i=e.posterUrl||e.image||'data:image/svg+xml,%3Csvg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 300 450"%3E%3Crect fill="%23222" width="300" height="450"/%3E%3Ctext x="50%25" y="50%25" fill="%23666" font-size="24" text-anchor="middle" dominant-baseline="middle"%3ENo Image%3C/text%3E%3C/svg%3E',n=e.backdropUrl||e.expandedImage||i,l=Array.isArray(e.genres)?e.genres.join(", "):e.genre||"Unknown",a=e.releaseDate?new Date(e.releaseDate).getFullYear():e.year||"N/A",r=e.runtime?`${e.runtime}m`:e.duration||"N/A",c=e.voteAverage?`★ ${e.voteAverage.toFixed(1)}`:e.rating||"N/A";o.innerHTML=`
        <div class="movie-poster-container">
          <img data-src="${i}" alt="${e.title}" class="movie-poster movie-poster-regular" loading="lazy" src="data:image/svg+xml,%3Csvg xmlns='http://www.w3.org/2000/svg' viewBox='0 0 300 450'%3E%3Crect fill='%23333' width='300' height='450'/%3E%3C/svg%3E" />
          <img data-src="${n}" alt="${e.title}" class="movie-poster movie-poster-expanded" loading="lazy" src="data:image/svg+xml,%3Csvg xmlns='http://www.w3.org/2000/svg' viewBox='0 0 1920 1080'%3E%3Crect fill='%23333' width='1920' height='1080'/%3E%3C/svg%3E" />
        </div>
        <div class="movie-overlay"></div>
        <div class="movie-compact-title">${e.title}</div>
        <div class="movie-info">
          <h3 class="movie-title">${e.title}</h3>
          <div class="movie-meta">
            <span>${l}</span>
            <span>${a}</span>
            <span>${r}</span>
            <span>${c}</span>
          </div>
          <p class="movie-description">${e.overview||e.description||"No description available."}</p>
        </div>
      `,movieHub.appendChild(o),row.appendChild(movieHub),this.setupLazyLoading(),this.setupCardHandlers()}setupScrollHandler(){const e=()=>{if(!this.topNav)return;const t=640*.45;window.scrollY>t?this.topNav.classList.add("is-solid"):this.topNav.classList.remove("is-solid")};e(),window.addEventListener("scroll",e,{passive:!0})}createContentCard(e,t,s=!1){const o=document.createElement("article");o.className="movie-card",o.dataset.index=t,o.dataset.contentId=e.id||e.tmdbId,o.dataset.contentType=e.type,o.dataset.isDiscovery=s;const i=e.posterUrl||e.image||'data:image/svg+xml,%3Csvg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 300 450"%3E%3Crect fill="%23222" width="300" height="450"/%3E%3Ctext x="50%25" y="50%25" fill="%23666" font-size="24" text-anchor="middle" dominant-baseline="middle"%3ENo Image%3C/text%3E%3C/svg%3E',n=e.backdropUrl||e.expandedImage||i,l=Array.isArray(e.genres)&&e.genres.length>0?e.genres.slice(0,2).join(", "):e.genre||"",a=e.releaseDate?new Date(e.releaseDate).getFullYear():e.year||"",r=e.runtime?`${e.runtime}m`:e.duration||"",c=e.voteAverage?`★ ${e.voteAverage.toFixed(1)}`:e.rating||"",d=[l,a,r,c].filter(Boolean);return o.innerHTML=`
      <div class="movie-poster-container">
        <img data-src="${i}" alt="${e.title}" class="movie-poster movie-poster-regular" loading="lazy" src="data:image/svg+xml,%3Csvg xmlns='http://www.w3.org/2000/svg' viewBox='0 0 300 450'%3E%3Crect fill='%23333' width='300' height='450'/%3E%3C/svg%3E" />
        <img data-src="${n}" alt="${e.title}" class="movie-poster movie-poster-expanded" loading="lazy" src="data:image/svg+xml,%3Csvg xmlns='http://www.w3.org/2000/svg' viewBox='0 0 1920 1080'%3E%3Crect fill='%23333' width='1920' height='1080'/%3E%3C/svg%3E" />
      </div>
      <div class="movie-overlay"></div>
      <div class="movie-compact-title">${e.title}</div>
      <div class="movie-info">
        <h3 class="movie-title">${e.title}</h3>
        <div class="movie-meta">
          ${d.map(h=>`<span>${h}</span>`).join("")}
        </div>
        <p class="movie-description">${e.overview||e.description||"No description available."}</p>
      </div>
    `,o}setupLazyLoading(){return this.imageObserver&&this.imageObserver.disconnect(),this.imageObserver=new IntersectionObserver((t,s)=>{t.forEach(o=>{if(o.isIntersecting){const i=o.target,n=i.dataset.src;n&&(i.src=n,i.removeAttribute("data-src"),s.unobserve(i))}})},{rootMargin:"200px 200px",threshold:.01}),document.querySelectorAll("img[data-src]").forEach(t=>{this.imageObserver.observe(t)}),this.imageObserver}setupCardHandlers(){document.querySelectorAll(".movie-card").forEach((t,s)=>{t.setAttribute("tabindex","0"),t.dataset.cardIndex=s,t.addEventListener("click",()=>{const o=t.dataset.contentId,i=t.dataset.contentType,n=t.dataset.isDiscovery==="true";this.contentModal.show(o,i,n)}),t.addEventListener("keydown",o=>{if(o.key==="Enter"){o.preventDefault();const i=t.dataset.contentId,n=t.dataset.contentType,l=t.dataset.isDiscovery==="true";this.contentModal.show(i,n,l)}})})}handleCardClick(e){const t=e.dataset.contentId,s=e.dataset.contentType,o=e.dataset.isDiscovery==="true";this.contentModal.show(t,s,o)}addCardActions(e,t,s){}async handlePlayAction(e,t){console.log("Play:",e,t),u.getStreamUrl(e),window.location.href=`player.html?contentId=${e}&type=${t}`}async handleQueueAction(e,t){await this.contentModal.show(e,t,!0)}async handleInfoAction(e,t){const o=document.querySelector(`[data-content-id="${e}"]`)?.dataset.isDiscovery==="true";await this.contentModal.show(e,t,o)}getFocusedHeroElement(){return this.focusedHeroElement}async refreshContent(){p.clearCache(),await this.loadContent(),this.createCarouselItems(),this.updateCarouselPosition(),this.updateAmbilightForCurrentSlide(),await this.renderCards("all")}getContentData(){return this.contentData}showDiscoveryOfflinePage(){const e=document.querySelector(".hero-stage");e&&(e.style.display="none");const t=document.querySelector(".spotlight");t&&(t.style.display="none");const s=document.querySelector(".content-shell");s&&(s.innerHTML=`
        <div style="
          display: flex;
          flex-direction: column;
          align-items: center;
          justify-content: center;
          min-height: 60vh;
          text-align: center;
          padding: 40px 20px;
        ">
          <h1 style="
            color: #fff;
            font-size: 48px;
            font-weight: 600;
            margin-bottom: 20px;
          ">Uh Oh!</h1>
          <p style="
            color: #fff;
            font-size: 24px;
            font-weight: 400;
            max-width: 600px;
            line-height: 1.5;
          ">Make sure you connected your server to the internet.</p>
          <button id="retry-discovery-btn" style="
            margin-top: 40px;
            background: #e50914;
            color: white;
            border: none;
            padding: 16px 40px;
            font-size: 18px;
            font-weight: 600;
            border-radius: 4px;
            cursor: pointer;
            transition: background 0.2s;
          " onmouseover="this.style.background='#f40612'" onmouseout="this.style.background='#e50914'">
            Retry Connection
          </button>
        </div>
      `,setTimeout(()=>{const o=document.getElementById("retry-discovery-btn");o&&o.addEventListener("click",async()=>{o.textContent="Checking...",o.disabled=!0,await u.checkConnection()?await this.switchCategory("discover"):(o.textContent="Still Offline - Try Again",o.disabled=!1)})},100))}showNormalUI(){const e=document.querySelector(".hero-stage");e&&(e.style.display="");const t=document.querySelector(".spotlight");t&&(t.style.display="");const s=document.querySelector(".content-shell");s&&!s.querySelector(".spotlight")&&(s.innerHTML=`
        <section class="spotlight">
          <div class="spotlight-header">
            <h2>Your Next Watch</h2>
            <div class="spotlight-tabs" role="tablist">
              <button class="tab active" data-tab="all">All</button>
              <button class="tab" data-tab="series">Series</button>
              <button class="tab" data-tab="movies">Movies</button>
            </div>
          </div>
          <div class="spotlight-row" id="spotlight-row"></div>
        </section>
      `,this.setupTabs())}setupTabs(){const e=document.querySelectorAll(".tab");e.forEach(t=>{t.addEventListener("click",async()=>{e.forEach(s=>s.classList.remove("active")),t.classList.add("active"),await this.renderCards(t.dataset.tab)})})}async renderHomePage(){const e=document.querySelector(".hero-stage"),t=document.querySelector(".content-shell");e.style.display="",this.createCarouselItems(),this.updateCarouselPosition(),this.updateAmbilightForCurrentSlide();const s=this.contentData.recentlyAdded||[],o=this.contentData.discoverPreview||[];t.innerHTML=`
      ${s.length>0?`
        <section class="spotlight" style="margin-top: 80px;">
          <div class="spotlight-header">
            <h2>Recently Added</h2>
          </div>
          <div class="spotlight-row">
            <div class="movie-hub" id="recently-added-hub"></div>
          </div>
        </section>
      `:""}
      ${o.length>0?`
        <section class="spotlight" style="margin-top: 80px;">
          <div class="spotlight-header" style="justify-content: space-between;">
            <h2>Discover New Content</h2>
            <button class="browse-all-btn" onclick="document.querySelector('[data-hero=\\'discover\\']').click()">
              Browse All →
            </button>
          </div>
          <div class="spotlight-row">
            <div class="movie-hub" id="discover-preview-hub"></div>
          </div>
        </section>
      `:""}
      ${s.length===0&&o.length===0?`
        <div style="text-align: center; padding: 60px 20px; color: #999;">
          <h2 style="color: #fff; margin-bottom: 20px;">Your Library is Empty</h2>
          <p style="font-size: 18px;">Go to Discovery to find and download content!</p>
        </div>
      `:""}
    `,s.length>0&&this.renderCarouselHub("recently-added-hub",s,!1),o.length>0&&this.renderCarouselHub("discover-preview-hub",o,!0),this.setupLazyLoading(),this.setupCardHandlers()}async renderDiscoverPage(){const e=document.querySelector(".hero-stage"),t=document.querySelector(".content-shell");e.style.display="",this.createCarouselItems(),this.updateCarouselPosition(),this.updateAmbilightForCurrentSlide();const s=this.contentData.trending||{movies:[],series:[]},o=Array.isArray(this.contentData.popularMovies)?this.contentData.popularMovies:this.contentData.popularMovies?.items||[],i=Array.isArray(this.contentData.popularSeries)?this.contentData.popularSeries:this.contentData.popularSeries?.items||[];t.innerHTML=`
      <div style="margin-top: 80px;">

        ${s.movies.length>0?`
          <section class="spotlight">
            <div class="spotlight-header">
              <h2>🔥 Trending Movies</h2>
            </div>
            <div class="spotlight-row">
              <div class="movie-hub" id="trending-movies-hub"></div>
            </div>
          </section>
        `:""}

        ${s.series.length>0?`
          <section class="spotlight">
            <div class="spotlight-header">
              <h2>📺 Trending Series</h2>
            </div>
            <div class="spotlight-row">
              <div class="movie-hub" id="trending-series-hub"></div>
            </div>
          </section>
        `:""}

        ${o.length>0?`
          <section class="spotlight">
            <div class="spotlight-header">
              <h2>⭐ Popular Movies</h2>
            </div>
            <div class="spotlight-row">
              <div class="movie-hub" id="popular-movies-hub"></div>
            </div>
          </section>
        `:""}

        ${i.length>0?`
          <section class="spotlight">
            <div class="spotlight-header">
              <h2>🎬 Popular Series</h2>
            </div>
            <div class="spotlight-row">
              <div class="movie-hub" id="popular-series-hub"></div>
            </div>
          </section>
        `:""}
      </div>
    `,this.renderCarouselHub("trending-movies-hub",s.movies,!0),this.renderCarouselHub("trending-series-hub",s.series,!0),this.renderCarouselHub("popular-movies-hub",o,!0),this.renderCarouselHub("popular-series-hub",i,!0),this.setupLazyLoading(),this.setupCardHandlers()}async renderMoviesPage(){const e=document.querySelector(".hero-stage"),t=document.querySelector(".content-shell");e.style.display="",this.createCarouselItems(),this.updateCarouselPosition(),this.updateAmbilightForCurrentSlide();const s=this.contentData.movies||[];t.innerHTML=`
      <section class="spotlight" style="margin-top: 80px;">
        <div class="spotlight-header" style="flex-direction: column; align-items: flex-start; margin-bottom: 20px;">
          <h2 style="font-size: 36px; font-weight: 700;">Your Movies</h2>
          <div style="color: #999; font-size: 16px;">${s.length} movies in your library</div>
        </div>
        <div class="spotlight-row">
          <div class="movie-hub" id="movies-hub"></div>
        </div>
      </section>
    `,this.renderCarouselHub("movies-hub",s,!1),this.setupLazyLoading(),this.setupCardHandlers()}async renderShowsPage(){const e=document.querySelector(".hero-stage"),t=document.querySelector(".content-shell");e.style.display="",this.createCarouselItems(),this.updateCarouselPosition(),this.updateAmbilightForCurrentSlide();const s=this.contentData.series||[];t.innerHTML=`
      <section class="spotlight" style="margin-top: 80px;">
        <div class="spotlight-header" style="flex-direction: column; align-items: flex-start; margin-bottom: 20px;">
          <h2 style="font-size: 36px; font-weight: 700;">Your Series</h2>
          <div style="color: #999; font-size: 16px;">${s.length} series in your library</div>
        </div>
        <div class="spotlight-row">
          <div class="movie-hub" id="shows-hub"></div>
        </div>
      </section>
    `,this.renderCarouselHub("shows-hub",s,!1),this.setupLazyLoading(),this.setupCardHandlers()}async renderMyListPage(){const e=document.querySelector(".hero-stage"),t=document.querySelector(".content-shell");e.style.display="",this.createCarouselItems(),this.updateCarouselPosition(),this.updateAmbilightForCurrentSlide();const s=this.contentData.watchlist||[];t.innerHTML=`
      <section class="spotlight" style="margin-top: 80px;">
        <div class="spotlight-header" style="flex-direction: column; align-items: flex-start; margin-bottom: 20px;">
          <h2 style="font-size: 36px; font-weight: 700;">My List</h2>
          <div style="color: #999; font-size: 16px;">${s.length} items in your list</div>
        </div>
        <div class="spotlight-row">
          <div class="movie-hub" id="mylist-hub"></div>
        </div>
      </section>
    `,this.renderCarouselHub("mylist-hub",s,!1),this.setupLazyLoading(),this.setupCardHandlers()}renderCarouselHub(e,t,s=!1){const o=document.getElementById(e);!o||!t||t.length===0||(o.innerHTML="",t.forEach((i,n)=>{const l=this.createContentCard(i,n,s);o.appendChild(l)}))}}class L{constructor(e,t){this.contentDisplay=e,this.profileManager=t,this.focusedElement="menu",this.focusedMenuIndex=1,this.focusedTabIndex=0,this.focusedCardIndex=0,this.focusedCarouselIndex=0,this.lastMenuIndex=1,this.isTransitioning=!1,this.transitionDuration=300,this.isTouchDevice=this.detectTouchDevice(),this.isAndroidTV=this.detectAndroidTV()}detectTouchDevice(){return("ontouchstart"in window||navigator.maxTouchPoints>0||navigator.msMaxTouchPoints>0)&&window.innerWidth<1024}detectAndroidTV(){const e=navigator.userAgent.toLowerCase();return e.includes("android")&&(e.includes("tv")||e.includes("aftm")||e.includes("aftb"))}initialize(){this.setupMenu(),this.setupTabs(),this.setupKeyboardNavigation(),this.setupServerStatusListener()}setupServerStatusListener(){let e=!1;window.addEventListener("server-limited-mode",t=>{if(e)return;e=!0;const s=t.detail.message;this.showNotification(s,"warning",8e3)})}showNotification(e,t="info",s=5e3){const o=document.querySelector(".server-notification");o&&o.remove();const i=document.createElement("div");if(i.className=`server-notification ${t}`,i.innerHTML=`
      <div class="notification-content">
        <span class="notification-icon">${t==="warning"?"⚠️":"ℹ️"}</span>
        <span class="notification-message">${e}</span>
      </div>
    `,i.style.cssText=`
      position: fixed;
      top: 80px;
      left: 50%;
      transform: translateX(-50%);
      background: ${t==="warning"?"rgba(255, 193, 7, 0.95)":"rgba(33, 150, 243, 0.95)"};
      color: ${t==="warning"?"#000":"#fff"};
      padding: 16px 24px;
      border-radius: 8px;
      box-shadow: 0 4px 12px rgba(0, 0, 0, 0.3);
      z-index: 10000;
      font-size: 14px;
      max-width: 600px;
      animation: slideDown 0.3s ease-out;
    `,!document.querySelector("#notification-styles")){const n=document.createElement("style");n.id="notification-styles",n.textContent=`
        @keyframes slideDown {
          from {
            opacity: 0;
            transform: translateX(-50%) translateY(-20px);
          }
          to {
            opacity: 1;
            transform: translateX(-50%) translateY(0);
          }
        }
        @keyframes slideUp {
          from {
            opacity: 1;
            transform: translateX(-50%) translateY(0);
          }
          to {
            opacity: 0;
            transform: translateX(-50%) translateY(-20px);
          }
        }
        .notification-content {
          display: flex;
          align-items: center;
          gap: 12px;
        }
        .notification-icon {
          font-size: 20px;
        }
        .notification-message {
          flex: 1;
        }
      `,document.head.appendChild(n)}document.body.appendChild(i),s>0&&setTimeout(()=>{i.style.animation="slideUp 0.3s ease-out",setTimeout(()=>i.remove(),300)},s)}setupMenu(){const e=document.querySelectorAll(".menu-item");e.forEach(o=>{o.id==="search-btn"||o.classList.contains("search-home")||o.addEventListener("click",async()=>{this.isTransitioning||(e.forEach(i=>i.classList.remove("active")),o.classList.add("active"),await this.navigateToPage(o.dataset.hero))})});const t=document.querySelector(".profile");t&&t.addEventListener("click",()=>{window.location.href="profiles.html"});const s=document.querySelector(".settings-btn");s&&s.addEventListener("click",()=>{window.location.href="settings.html"})}async navigateToPage(e){if(this.isTransitioning)return;this.isTransitioning=!0;const t=document.querySelector("main");try{t&&(t.style.transition=`opacity ${this.transitionDuration}ms ease-out`,t.style.opacity="0"),await this.delay(this.transitionDuration),await this.contentDisplay.switchCategory(e),t&&(t.style.opacity="1"),await this.delay(this.transitionDuration)}catch(s){console.error("Navigation error:",s),t&&(t.style.opacity="1")}finally{this.isTransitioning=!1}}delay(e){return new Promise(t=>setTimeout(t,e))}setupTabs(){const e=document.querySelectorAll(".tab");e.forEach(t=>{t.addEventListener("click",async()=>{e.forEach(s=>s.classList.remove("active")),t.classList.add("active"),await this.contentDisplay.renderCards(t.dataset.tab)})})}setupKeyboardNavigation(){if(!this.isTouchDevice||this.isAndroidTV){const e=Array.from(document.querySelectorAll(".menu-item"));e.forEach(t=>t.classList.remove("active")),e[this.focusedMenuIndex]&&e[this.focusedMenuIndex].classList.add("active"),document.addEventListener("keydown",t=>this.handleKeyboard(t)),this.isAndroidTV&&this.setupRemoteControlSupport(),this.updateFocus()}else this.setupTouchNavigation()}setupTouchNavigation(){document.body.classList.add("touch-device");const e=document.createElement("style");e.textContent=`
      .touch-device .focused {
        outline: none !important;
        box-shadow: none !important;
        transform: none !important;
      }
      .touch-device .movie-card.expanded {
        transform: none !important;
      }
    `,document.head.appendChild(e)}setupRemoteControlSupport(){const e={MediaPlayPause:" ",MediaPlay:" ",MediaPause:" ",MediaStop:"Escape",MediaTrackNext:"ArrowRight",MediaTrackPrevious:"ArrowLeft",Back:"Escape"};document.addEventListener("keydown",t=>{if(e[t.key]){const s=e[t.key],o=new KeyboardEvent("keydown",{key:s,code:s,bubbles:!0,cancelable:!0});t.preventDefault(),document.dispatchEvent(o)}})}handleKeyboard(e){if(this.isTouchDevice&&!this.isAndroidTV)return;if(["ArrowUp","ArrowDown","ArrowLeft","ArrowRight"].includes(e.key)&&e.preventDefault(),this.profileManager.profileSelectionActive){this.profileManager.handleKeyboard(e);return}const t=Array.from(document.querySelectorAll(".menu-item")),s=Array.from(document.querySelectorAll(".tab"));if(this.focusedElement==="hero")if(e.key==="ArrowLeft"){const o=this.contentDisplay.currentHeroIndex>0?this.contentDisplay.currentHeroIndex-1:this.contentDisplay.currentHeroIndex;this.contentDisplay.goToSlide(o)}else if(e.key==="ArrowRight"){const o=this.contentDisplay.currentHeroIndex+1;this.contentDisplay.goToSlide(o)}else e.key==="ArrowUp"?(this.focusedElement="menu",this.focusedMenuIndex=this.lastMenuIndex,this.updateFocus()):e.key==="ArrowDown"&&(this.focusedElement="tabs",this.updateFocus());else if(this.focusedElement==="menu")e.key==="ArrowLeft"?this.focusedMenuIndex===0?(this.focusedElement="profile",this.updateFocus()):(this.focusedMenuIndex=this.focusedMenuIndex>0?this.focusedMenuIndex-1:t.length-1,t.forEach(o=>o.classList.remove("active")),t[this.focusedMenuIndex].classList.add("active"),this.navigateToPage(t[this.focusedMenuIndex].dataset.hero).catch(console.error),this.updateFocus()):e.key==="ArrowRight"?this.focusedMenuIndex===t.length-1?(this.focusedElement="settings",this.updateFocus()):(this.focusedMenuIndex=this.focusedMenuIndex<t.length-1?this.focusedMenuIndex+1:0,t.forEach(o=>o.classList.remove("active")),t[this.focusedMenuIndex].classList.add("active"),this.navigateToPage(t[this.focusedMenuIndex].dataset.hero).catch(console.error),this.updateFocus()):e.key==="ArrowDown"?(this.lastMenuIndex=this.focusedMenuIndex,this.focusedElement="hero",this.updateFocus()):e.key==="Enter"&&(this.lastMenuIndex=this.focusedMenuIndex,this.focusedElement="hero",this.updateFocus());else if(this.focusedElement==="profile")e.key==="ArrowRight"?(this.focusedElement="menu",this.focusedMenuIndex=0,this.updateFocus()):e.key==="ArrowDown"?(this.focusedElement="hero",this.updateFocus()):e.key==="Enter"&&(window.location.href="profiles.html");else if(this.focusedElement==="settings")e.key==="ArrowLeft"?(this.focusedElement="menu",this.focusedMenuIndex=t.length-1,this.updateFocus()):e.key==="ArrowDown"?(this.focusedElement="hero",this.updateFocus()):e.key==="Enter"&&(window.location.href="settings.html");else if(this.focusedElement==="tabs")e.key==="ArrowLeft"?(this.focusedTabIndex=this.focusedTabIndex>0?this.focusedTabIndex-1:s.length-1,this.updateFocus()):e.key==="ArrowRight"?(this.focusedTabIndex=this.focusedTabIndex<s.length-1?this.focusedTabIndex+1:0,this.updateFocus()):e.key==="ArrowUp"?(this.focusedElement="hero",this.updateFocus()):e.key==="ArrowDown"?(this.focusedElement="cards",this.focusedCardIndex=0,this.focusedCarouselIndex=0,this.updateFocus()):e.key==="Enter"&&(s[this.focusedTabIndex].click(),this.updateFocus());else if(this.focusedElement==="cards"){const o=this.getCarousels(),i=o[this.focusedCarouselIndex];if(!i)return;const n=Array.from(i.querySelectorAll(".movie-card"));e.key==="ArrowLeft"?(this.focusedCardIndex=this.focusedCardIndex>0?this.focusedCardIndex-1:n.length-1,this.updateFocus()):e.key==="ArrowRight"?(this.focusedCardIndex=this.focusedCardIndex<n.length-1?this.focusedCardIndex+1:0,this.updateFocus()):e.key==="ArrowUp"?this.focusedCarouselIndex>0?(this.focusedCarouselIndex--,this.focusedCardIndex=0,this.updateFocus()):(this.focusedElement="tabs",this.updateFocus()):e.key==="ArrowDown"&&this.focusedCarouselIndex<o.length-1&&(this.focusedCarouselIndex++,this.focusedCardIndex=0,this.updateFocus())}}getCarousels(){return Array.from(document.querySelectorAll(".movie-hub"))}updateFocus(){const e=Array.from(document.querySelectorAll(".menu-item")),t=Array.from(document.querySelectorAll(".tab")),s=()=>Array.from(document.querySelectorAll(".movie-card")),o=document.querySelector(".profile"),i=document.querySelector(".settings-btn");if(document.querySelectorAll(".hero").forEach(a=>a.classList.remove("focused")),e.forEach(a=>a.classList.remove("focused")),t.forEach(a=>a.classList.remove("focused")),o&&o.classList.remove("focused"),i&&i.classList.remove("focused"),s().forEach(a=>{a.classList.remove("focused"),a.classList.remove("expanded");const r=a.querySelector(".movie-title");r&&(r.style.textShadow="")}),this.focusedElement==="hero"){const a=this.contentDisplay.getFocusedHeroElement();a&&a.classList.add("focused")}else if(this.focusedElement==="menu")e[this.focusedMenuIndex]&&e[this.focusedMenuIndex].classList.add("focused");else if(this.focusedElement==="profile")o&&o.classList.add("focused");else if(this.focusedElement==="settings")i&&i.classList.add("focused");else if(this.focusedElement==="tabs")t[this.focusedTabIndex]&&t[this.focusedTabIndex].classList.add("focused");else if(this.focusedElement==="cards"){const r=this.getCarousels()[this.focusedCarouselIndex];if(r){const c=Array.from(r.querySelectorAll(".movie-card"));if(c[this.focusedCardIndex]){const d=c[this.focusedCardIndex];d.classList.add("focused"),d.classList.add("expanded");const h=d.querySelector(".movie-title");h&&(h.style.textShadow=`
              0 0 20px rgba(255, 255, 255, 0.8),
              0 0 40px rgba(255, 255, 255, 0.6),
              0 0 60px rgba(255, 255, 255, 0.4)
            `),this.updateMovieCarousel(r)}}}}updateMovieCarousel(e){if(!e)return;const t=Array.from(e.querySelectorAll(".movie-card"));if(t.length>0){const s=window.innerWidth<=768,o=window.innerWidth<=480,i=o?120:s?140:180,n=o?320:s?380:480,l=o?12:16;let a=0;for(let r=0;r<this.focusedCardIndex;r++){const d=t[r].classList.contains("expanded");a+=(d?n:i)+l}e.style.transform=`translateX(-${a}px)`}}}const I="modulepreload",M=function(v){return"/"+v},w={},A=function(e,t,s){let o=Promise.resolve();if(t&&t.length>0){document.getElementsByTagName("link");const n=document.querySelector("meta[property=csp-nonce]"),l=n?.nonce||n?.getAttribute("nonce");o=Promise.allSettled(t.map(a=>{if(a=M(a),a in w)return;w[a]=!0;const r=a.endsWith(".css"),c=r?'[rel="stylesheet"]':"";if(document.querySelector(`link[href="${a}"]${c}`))return;const d=document.createElement("link");if(d.rel=r?"stylesheet":I,r||(d.as="script"),d.crossOrigin="",d.href=a,l&&d.setAttribute("nonce",l),document.head.appendChild(d),r)return new Promise((h,f)=>{d.addEventListener("load",h),d.addEventListener("error",()=>f(new Error(`Unable to preload CSS for ${a}`)))})}))}function i(n){const l=new Event("vite:preloadError",{cancelable:!0});if(l.payload=n,window.dispatchEvent(l),!l.defaultPrevented)throw n}return o.then(n=>{for(const l of n||[])l.status==="rejected"&&i(l.reason);return e().catch(i)})};class k{constructor(){this.searchInput=null,this.searchResults=null,this.searchOverlay=null,this.debounceTimer=null,this.debounceDelay=300,this.isOpen=!1}initialize(){this.createSearchUI(),this.attachEventListeners()}createSearchUI(){this.searchOverlay=document.createElement("div"),this.searchOverlay.className="search-overlay",this.searchOverlay.innerHTML=`
      <div class="search-modal">
        <div class="search-header">
          <div class="search-input-wrapper">
            <svg class="search-icon" viewBox="0 0 24 24" width="20" height="20">
              <path fill="currentColor" d="M15.5 14h-.79l-.28-.27C15.41 12.59 16 11.11 16 9.5 16 5.91 13.09 3 9.5 3S3 5.91 3 9.5 5.91 16 9.5 16c1.61 0 3.09-.59 4.23-1.57l.27.28v.79l5 4.99L20.49 19l-4.99-5zm-6 0C7.01 14 5 11.99 5 9.5S7.01 5 9.5 5 14 7.01 14 9.5 11.99 14 9.5 14z"/>
            </svg>
            <input type="text" class="search-input" placeholder="Search movies and TV shows..." autocomplete="off">
            <button class="search-clear-btn" style="display: none;">
              <svg viewBox="0 0 24 24" width="20" height="20">
                <path fill="currentColor" d="M19 6.41L17.59 5 12 10.59 6.41 5 5 6.41 10.59 12 5 17.59 6.41 19 12 13.41 17.59 19 19 17.59 13.41 12z"/>
              </svg>
            </button>
          </div>
          <button class="search-close-btn">
            <svg viewBox="0 0 24 24" width="24" height="24">
              <path fill="currentColor" d="M19 6.41L17.59 5 12 10.59 6.41 5 5 6.41 10.59 12 5 17.59 6.41 19 12 13.41 17.59 19 19 17.59 13.41 12z"/>
            </svg>
          </button>
        </div>
        <div class="search-results">
          <div class="search-empty">
            <svg viewBox="0 0 24 24" width="48" height="48">
              <path fill="currentColor" d="M15.5 14h-.79l-.28-.27C15.41 12.59 16 11.11 16 9.5 16 5.91 13.09 3 9.5 3S3 5.91 3 9.5 5.91 16 9.5 16c1.61 0 3.09-.59 4.23-1.57l.27.28v.79l5 4.99L20.49 19l-4.99-5zm-6 0C7.01 14 5 11.99 5 9.5S7.01 5 9.5 5 14 7.01 14 9.5 11.99 14 9.5 14z"/>
            </svg>
            <p>Start typing to search...</p>
          </div>
        </div>
      </div>
    `,document.body.appendChild(this.searchOverlay),this.searchInput=this.searchOverlay.querySelector(".search-input"),this.searchResults=this.searchOverlay.querySelector(".search-results")}attachEventListeners(){this.searchInput.addEventListener("input",e=>{this.handleSearchInput(e.target.value)}),this.searchOverlay.querySelector(".search-clear-btn").addEventListener("click",()=>{this.clearSearch()}),this.searchOverlay.querySelector(".search-close-btn").addEventListener("click",()=>{this.close()}),this.searchOverlay.addEventListener("click",e=>{e.target===this.searchOverlay&&this.close()}),document.addEventListener("keydown",e=>{e.key==="/"&&!this.isOpen&&document.activeElement.tagName!=="INPUT"&&(e.preventDefault(),this.open()),e.key==="Escape"&&this.isOpen&&this.close()})}handleSearchInput(e){const t=this.searchOverlay.querySelector(".search-clear-btn");if(e.length>0)t.style.display="block";else{t.style.display="none",this.showEmptyState();return}clearTimeout(this.debounceTimer),this.debounceTimer=setTimeout(()=>{this.performSearch(e)},this.debounceDelay)}async performSearch(e){if(e.trim().length<2){this.showEmptyState("Please enter at least 2 characters");return}try{this.showLoading();const t=await u.searchTMDB(e);let s=[];Array.isArray(t)?s=t:t&&Array.isArray(t.results)?s=t.results:t&&t.data&&Array.isArray(t.data)&&(s=t.data),s.length===0?this.showEmptyState("No results found"):this.displayResults(s)}catch(t){console.error("Search failed:",t),this.showEmptyState("Search failed. Please try again.")}}displayResults(e){if(!Array.isArray(e)){console.error("Search results is not an array:",e),this.showEmptyState("Invalid search results");return}const t=document.createElement("div");t.className="search-results-grid",e.forEach(s=>{const o=this.createResultCard(s);o.addEventListener("click",()=>{this.handleResultClick(s)}),t.appendChild(o)}),this.searchResults.innerHTML="",this.searchResults.appendChild(t)}createResultCard(e){const t=e.posterUrl||null,s=e.releaseDate?new Date(e.releaseDate).getFullYear():"",o=e.type==="movie"?"Movie":"TV Show",i=e.voteAverage?e.voteAverage.toFixed(1):"N/A",n=document.createElement("div");n.className="search-result-card",n.dataset.id=e.id;const l=document.createElement("div");l.className="search-result-poster";const a=document.createElement("img");t?(a.src=t,a.alt=e.title,a.loading="lazy"):(a.src="data:image/svg+xml;charset=utf-8,"+encodeURIComponent(`
        <svg xmlns="http://www.w3.org/2000/svg" width="342" height="513" viewBox="0 0 342 513">
          <rect fill="#222" width="342" height="513"/>
          <text x="50%" y="50%" dominant-baseline="middle" text-anchor="middle" font-family="sans-serif" font-size="24" fill="#666">No Image</text>
        </svg>
      `),a.alt=e.title);const r=document.createElement("div");r.className="search-result-overlay",r.innerHTML=`
      <svg viewBox="0 0 24 24" width="48" height="48">
        <path fill="white" d="M8 5v14l11-7z"/>
      </svg>
    `,l.appendChild(a),l.appendChild(r);const c=document.createElement("div");c.className="search-result-info";const d=document.createElement("h3");d.className="search-result-title",d.textContent=e.title;const h=document.createElement("div");h.className="search-result-meta";const f=document.createElement("span");if(f.className="search-result-type",f.textContent=o,h.appendChild(f),s){const g=document.createElement("span");g.className="search-result-year",g.textContent=s,h.appendChild(g)}const m=document.createElement("span");return m.className="search-result-rating",m.innerHTML=`
      <svg viewBox="0 0 24 24" width="14" height="14">
        <path fill="currentColor" d="M12 17.27L18.18 21l-1.64-7.03L22 9.24l-7.19-.61L12 2 9.19 8.63 2 9.24l5.46 4.73L5.82 21z"/>
      </svg>
      ${i}
    `,h.appendChild(m),c.appendChild(d),c.appendChild(h),n.appendChild(l),n.appendChild(c),n}handleResultClick(e){this.close(),A(()=>Promise.resolve().then(()=>E),void 0).then(t=>{const s=t.default,o={selectedProfileId:p.currentProfileId};new s(o).show(e.id,e.type,!0)})}showLoading(){this.searchResults.innerHTML=`
      <div class="search-loading">
        <div class="spinner"></div>
        <p>Searching...</p>
      </div>
    `}showEmptyState(e="Start typing to search..."){this.searchResults.innerHTML=`
      <div class="search-empty">
        <svg viewBox="0 0 24 24" width="48" height="48">
          <path fill="currentColor" d="M15.5 14h-.79l-.28-.27C15.41 12.59 16 11.11 16 9.5 16 5.91 13.09 3 9.5 3S3 5.91 3 9.5 5.91 16 9.5 16c1.61 0 3.09-.59 4.23-1.57l.27.28v.79l5 4.99L20.49 19l-4.99-5zm-6 0C7.01 14 5 11.99 5 9.5S7.01 5 9.5 5 14 7.01 14 9.5 11.99 14 9.5 14z"/>
        </svg>
        <p>${e}</p>
      </div>
    `}clearSearch(){this.searchInput.value="",this.searchOverlay.querySelector(".search-clear-btn").style.display="none",this.showEmptyState(),this.searchInput.focus()}open(){this.searchOverlay.classList.add("active"),this.isOpen=!0,this.searchInput.focus()}close(){this.searchOverlay.classList.remove("active"),this.isOpen=!1,this.clearSearch()}}const b=new k;class D{constructor(){this.badge=null,this.updateInterval=null}init(){this.badge=document.getElementById("notification-badge"),this.badge&&(this.updateBadge(),this.updateInterval=setInterval(()=>{this.updateBadge()},3e4))}async updateBadge(){try{const e=this.getCurrentProfileId(),s=((await u.get(`/api/notifications/${e}`)).notifications||[]).filter(o=>!o.userResponse).length;this.badge&&(s>0?(this.badge.textContent=s>99?"99+":s,this.badge.classList.add("active")):this.badge.classList.remove("active"))}catch(e){console.debug("Failed to update notification badge:",e)}}getCurrentProfileId(){return parseInt(localStorage.getItem("selectedProfileId")||"1",10)}destroy(){this.updateInterval&&clearInterval(this.updateInterval)}}const $=new D;function T(){return!localStorage.getItem("lanflix_config")&&!window.location.pathname.includes("app-config.html")?(window.location.replace("app-config.html"),!1):!0}function H(){return window.Capacitor!==void 0}document.addEventListener("DOMContentLoaded",async()=>{if(!(H()&&!T()))try{if(!p.currentProfileId){window.location.replace("profiles.html");return}const v=new x,e=new S(v),t=new L(e,v);await v.initialize(),v.selectedProfileId=p.currentProfileId;const s=v.profiles.find(i=>i.id===p.currentProfileId);if(s){const i=document.querySelector(".profile-avatar");i&&(i.style.background=`linear-gradient(135deg, ${s.avatarColorPrimary}, ${s.avatarColorSecondary})`)}await e.initialize(),t.initialize(),C.initialize(),b.initialize();const o=document.getElementById("search-btn");o&&o.addEventListener("click",()=>{b.open()}),$.init()}catch(v){console.error("Failed to initialize application:",v),document.body.innerHTML=`
      <div style="display: flex; align-items: center; justify-content: center; height: 100vh; color: white; text-align: center; padding: 20px;">
        <div>
          <h1>Failed to load application</h1>
          <p>Please check your connection and try again.</p>
          <button onclick="location.reload()" style="margin-top: 20px; padding: 10px 20px; font-size: 16px; cursor: pointer;">
            Retry
          </button>
        </div>
      </div>
    `}});

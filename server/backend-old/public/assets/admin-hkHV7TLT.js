import{a as s}from"./api-client-7IhkegDa.js";/* empty css             */async function v(){try{const e=(await s.getSettings()).settings||{};document.getElementById("movies-path").value=e.moviesPath||"/media/movies",document.getElementById("series-path").value=e.seriesPath||"/media/series",document.getElementById("tmdb-key").value=e.tmdbApiKey||"",document.getElementById("sonarr-url").value=e.sonarrUrl||"",document.getElementById("sonarr-key").value=e.sonarrApiKey||"",document.getElementById("radarr-url").value=e.radarrUrl||"",document.getElementById("radarr-key").value=e.radarrApiKey||"",document.getElementById("prowlarr-url").value=e.prowlarrUrl||"",document.getElementById("prowlarr-key").value=e.prowlarrApiKey||"",document.getElementById("auto-metadata").checked=e.autoMetadata!==!1,document.getElementById("download-images").checked=e.downloadImages!==!1,document.getElementById("metadata-language").value=e.metadataLanguage||"en"}catch(t){console.error("Failed to load settings:",t),r("Failed to load current settings","error")}}async function g(){const t=document.getElementById("save-btn");t.textContent="⏳ Saving...",t.disabled=!0;try{const e={moviesPath:document.getElementById("movies-path").value,seriesPath:document.getElementById("series-path").value,tmdbApiKey:document.getElementById("tmdb-key").value,sonarrUrl:document.getElementById("sonarr-url").value,sonarrApiKey:document.getElementById("sonarr-key").value,radarrUrl:document.getElementById("radarr-url").value,radarrApiKey:document.getElementById("radarr-key").value,prowlarrUrl:document.getElementById("prowlarr-url").value,prowlarrApiKey:document.getElementById("prowlarr-key").value,autoMetadata:document.getElementById("auto-metadata").checked,downloadImages:document.getElementById("download-images").checked,metadataLanguage:document.getElementById("metadata-language").value};await s.updateSettings(e),r("✅ Configuration saved successfully!","success"),t.textContent="💾 Save Configuration",t.disabled=!1}catch(e){console.error("Failed to save settings:",e),r("❌ Failed to save configuration: "+e.message,"error"),t.textContent="💾 Save Configuration",t.disabled=!1}}async function f(t){const e=document.getElementById("status");e.textContent=`Testing ${t} connection...`,e.className="",e.style.display="block";try{const i=await s.testServiceConnection(t);i.connected?r(`✅ ${t} connected successfully!`,"success"):r(`❌ ${t} connection failed: ${i.error||"Unknown error"}`,"error")}catch(i){r(`❌ ${t} connection failed: ${i.message}`,"error")}}window.togglePassword=function(t){const e=document.getElementById(t);e.type=e.type==="password"?"text":"password"};function r(t,e){const i=document.getElementById("status");i.textContent=t,i.className=e,i.style.display="block",e==="success"&&setTimeout(()=>{i.style.display="none"},5e3)}async function b(){const t=document.getElementById("scan-library-btn"),e=document.getElementById("scan-status");t.textContent="⏳ Scanning...",t.disabled=!0,e.textContent="Scanning media folders for new content...",e.className="",e.style.color="#666";try{const i=await s.request("/jobs/library-scan/trigger",{method:"POST"});e.textContent="✅ Library scan completed! Refresh the page to see new content.",e.style.color="#4caf50",t.textContent="🔍 Scan Library Now",t.disabled=!1}catch(i){console.error("Failed to scan library:",i),e.textContent="❌ Failed to scan library: "+i.message,e.style.color="#f44336",t.textContent="🔍 Scan Library Now",t.disabled=!1}}window.testConnection=f;window.scanLibrary=b;function p(t){document.querySelectorAll(".admin-tab").forEach(e=>{e.classList.remove("active")}),document.querySelector(`[data-tab="${t}"]`).classList.add("active"),document.querySelectorAll(".admin-tab-content").forEach(e=>{e.classList.remove("active")}),document.getElementById(`${t}-tab`).classList.add("active"),t==="media"&&m()}async function m(){await Promise.all([h(),$()])}async function h(){const t=document.getElementById("movies-list");if(!t){console.error("movies-list element not found");return}t.innerHTML='<div class="loading">Loading movies...</div>';try{console.log("Fetching movies from API...");const e=await s.getLibraryMovies();console.log("Movies response:",e);const i=e.items||e.content||[];if(i.length===0){t.innerHTML='<div class="loading">No movies found in library. Add some movies to your library first!</div>';return}t.innerHTML=i.map(a=>`
      <div class="media-item" data-id="${a.id}">
        <img src="${a.posterUrl||"/placeholder.jpg"}" alt="${a.title}" class="media-poster" onerror="this.src='/placeholder.jpg'" />
        <div class="media-info">
          <div class="media-title">${a.title} ${a.year?`(${a.year})`:""}</div>
          <div class="media-meta">
            ${a.runtime?`${a.runtime} min`:""} 
            ${a.genres?`• ${a.genres.slice(0,2).join(", ")}`:""}
          </div>
          <div class="media-path" title="${a.filePath||"No file path"}">${a.filePath||"No file path"}</div>
        </div>
        <div class="media-actions">
          <button class="media-btn transcode" onclick="showTranscodeModal(${a.id}, 'movie', '${a.title}')">🎬 Transcode</button>
          <button class="media-btn edit" onclick="showEditModal(${a.id}, 'movie')">✏️ Edit</button>
          <button class="media-btn delete" onclick="deleteMedia(${a.id}, 'movie', '${a.title}')">🗑️ Delete</button>
        </div>
      </div>
    `).join("")}catch(e){console.error("Failed to load movies:",e),t.innerHTML='<div class="loading">Failed to load movies</div>'}}async function $(){const t=document.getElementById("series-list");if(!t){console.error("series-list element not found");return}t.innerHTML='<div class="loading">Loading series...</div>';try{console.log("Fetching series from API...");const e=await s.getLibrarySeries();console.log("Series response:",e);const i=e.items||e.content||[];if(i.length===0){t.innerHTML='<div class="loading">No series found in library. Add some series to your library first!</div>';return}t.innerHTML=i.map(a=>`
      <div class="media-item series-item" data-id="${a.id}">
        <img src="${a.posterUrl||"/placeholder.jpg"}" alt="${a.title}" class="media-poster" onerror="this.src='/placeholder.jpg'" />
        <div class="media-info">
          <div class="media-title">${a.title} ${a.year?`(${a.year})`:""}</div>
          <div class="media-meta">
            ${a.numberOfSeasons?`${a.numberOfSeasons} seasons`:""} 
            ${a.genres?`• ${a.genres.slice(0,2).join(", ")}`:""}
          </div>
          <div class="media-path" title="${a.folderPath||"No folder path"}">${a.folderPath||"No folder path"}</div>
        </div>
        <div class="media-actions">
          <button class="media-btn" onclick="toggleEpisodes(${a.id}, '${a.title}')">📺 Episodes</button>
          <button class="media-btn edit" onclick="showEditModal(${a.id}, 'series')">✏️ Edit</button>
          <button class="media-btn delete" onclick="deleteMedia(${a.id}, 'series', '${a.title}')">🗑️ Delete</button>
        </div>
      </div>
      <div class="episodes-container" id="episodes-${a.id}" style="display: none;"></div>
    `).join("")}catch(e){console.error("Failed to load series:",e),t.innerHTML='<div class="loading">Failed to load series</div>'}}window.toggleEpisodes=async function(t,e){const i=document.getElementById(`episodes-${t}`);if(i.style.display==="none"){i.innerHTML='<div class="loading" style="padding: 20px;">Loading episodes...</div>',i.style.display="block";try{const l=(await s.getLibraryItem(t)).episodes||[];if(l.length===0){i.innerHTML='<div class="loading" style="padding: 20px;">No episodes found</div>';return}const d={};l.forEach(o=>{d[o.seasonNumber]||(d[o.seasonNumber]=[]),d[o.seasonNumber].push(o)});let c='<div class="episodes-list">';Object.keys(d).sort((o,n)=>o-n).forEach(o=>{c+=`
          <div class="season-group">
            <h3 class="season-title">Season ${o}</h3>
            <div class="season-episodes">
        `,d[o].sort((n,y)=>n.episodeNumber-y.episodeNumber).forEach(n=>{c+=`
            <div class="episode-item">
              <div class="episode-info">
                <div class="episode-title">
                  ${n.episodeNumber}. ${n.title||`Episode ${n.episodeNumber}`}
                </div>
                <div class="episode-meta">
                  ${n.runtime?`${n.runtime} min`:""}
                  ${n.airDate?`• Aired: ${new Date(n.airDate).toLocaleDateString()}`:""}
                </div>
                <div class="media-path" title="${n.filePath||"No file"}">${n.filePath||"No file"}</div>
              </div>
              <div class="episode-actions">
                ${n.filePath?`<button class="media-btn transcode" onclick="showTranscodeModal(${n.id}, 'episode', 'S${o}E${n.episodeNumber} - ${n.title||"Episode"}')">🎬 Transcode</button>`:""}
              </div>
            </div>
          `}),c+=`
            </div>
          </div>
        `}),c+="</div>",i.innerHTML=c}catch(a){console.error("Failed to load episodes:",a),i.innerHTML='<div class="loading" style="padding: 20px; color: #f44336;">Failed to load episodes</div>'}}else i.style.display="none"};window.showTranscodeModal=async function(t,e,i){if(confirm(`Start maximum quality transcode for "${i}"?

🎬 Settings:
• GPU: RTX 4070 Ti (NVENC)
• Quality: CQ 16 (near-lossless)
• Preset: p7 (maximum)
• Audio: 320k AAC 5.1

Original will be backed up with .original extension.`))try{const a=await s.request("/transcode/offline",{method:"POST",body:JSON.stringify({contentId:t,type:e})});alert(`✅ Transcoding started for "${i}"!

Running in background with maximum quality settings.
Original will be backed up with .original extension.`)}catch(a){console.error("Failed to start transcoding:",a),alert(`❌ Failed to start transcoding: ${a.message}`)}};window.showEditModal=async function(t,e){try{const i=await s.getLibraryItem(t),a=document.createElement("div");a.className="edit-modal active",a.innerHTML=`
      <div class="edit-modal-content">
        <div class="edit-modal-header">
          <h2 class="edit-modal-title">Edit: ${i.title}</h2>
          <button class="edit-modal-close" onclick="this.closest('.edit-modal').remove()">×</button>
        </div>
        <div class="edit-modal-body">
          <div class="admin-field">
            <label>Title</label>
            <input type="text" id="edit-title" value="${i.title}" />
          </div>
          <div class="admin-field">
            <label>Year</label>
            <input type="text" id="edit-year" value="${i.year||""}" />
          </div>
          <div class="admin-field">
            <label>Overview</label>
            <textarea id="edit-overview" rows="4" style="width: 100%; background: rgba(255,255,255,0.1); border: 1px solid rgba(255,255,255,0.2); border-radius: 4px; padding: 12px; color: #fff; font-family: 'Poppins', sans-serif;">${i.overview||""}</textarea>
          </div>
        </div>
        <div class="edit-modal-footer">
          <button class="cancel-btn" onclick="this.closest('.edit-modal').remove()">Cancel</button>
          <button class="save-btn" onclick="saveMediaEdit(${t}, '${e}')">Save Changes</button>
        </div>
      </div>
    `,document.body.appendChild(a)}catch(i){console.error("Failed to load content details:",i),alert("Failed to load content details")}};window.saveMediaEdit=async function(t,e){const i=document.getElementById("edit-title").value,a=document.getElementById("edit-year").value,l=document.getElementById("edit-overview").value;try{await s.request(`/library/${t}`,{method:"PUT",body:JSON.stringify({title:i,year:a,overview:l})}),alert("✅ Changes saved successfully!"),document.querySelector(".edit-modal").remove(),m()}catch(d){console.error("Failed to save changes:",d),alert("❌ Failed to save changes: "+d.message)}};window.deleteMedia=async function(t,e,i){if(confirm(`Are you sure you want to delete "${i}"?

This will remove it from the library and delete the file from disk.`))try{await s.removeFromLibrary(t),alert(`✅ "${i}" has been deleted`),m()}catch(a){console.error("Failed to delete media:",a),alert("❌ Failed to delete: "+a.message)}};document.addEventListener("DOMContentLoaded",()=>{console.log("Admin page loaded"),v(),document.getElementById("save-btn").addEventListener("click",g),document.querySelectorAll(".admin-tab").forEach(e=>{e.addEventListener("click",()=>{console.log("Tab clicked:",e.dataset.tab),p(e.dataset.tab)})}),document.getElementById("movie-search")?.addEventListener("input",e=>{u("movies",e.target.value)}),document.getElementById("series-search")?.addEventListener("input",e=>{u("series",e.target.value)});const t=document.querySelector(".admin-tab.active");t&&t.dataset.tab==="media"&&(console.log("Media tab is active on load, loading media library..."),m())});function u(t,e){const i=t==="movies"?"movies-list":"series-list";document.querySelectorAll(`#${i} .media-item`).forEach(l=>{l.querySelector(".media-title").textContent.toLowerCase().includes(e.toLowerCase())?l.style.display="flex":l.style.display="none"})}

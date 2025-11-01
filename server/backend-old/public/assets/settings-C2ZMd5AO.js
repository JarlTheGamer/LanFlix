import{a as d}from"./api-client-7IhkegDa.js";/* empty css             */import{t as g}from"./tv-navigation-BRt9J30j.js";import{s as f}from"./data-DddsZ_ae.js";document.addEventListener("DOMContentLoaded",()=>{g.initialize()});class y{constructor(){this.focusedArea="back",this.focusedNavIndex=0,this.focusedContentIndex=0,this.selectMode=!1,this.currentSelectElement=null,this.selectOptionIndex=0,this.modalActive=!1,this.modalFocusIndex=0,this.selectedColor=null,this.currentProfileCard=null,this.settings={},this.profiles=[]}async initialize(){await this.loadSettings(),await this.loadProfiles(),this.setupNavigation(),this.initializeCustomSelects(),this.setupToggles(),this.setupModals(),this.setupProfiles(),this.updateFocus(),document.addEventListener("keydown",e=>this.handleKeyboard(e)),document.addEventListener("click",e=>{e.target.closest(".custom-select-wrapper")||this.closeAllDropdowns()})}async loadSettings(){try{const e=await d.getSettings();this.settings=e.settings||{},this.applySettings()}catch(e){console.error("Failed to load settings:",e),this.settings={}}}async loadProfiles(){try{const e=await f.getProfiles();this.profiles=e||[],this.renderProfiles()}catch(e){console.error("Failed to load profiles:",e),this.profiles=[]}}applySettings(){Object.keys(this.settings).forEach(e=>{const t=document.getElementById(e);t&&(t.type==="checkbox"?t.checked=this.settings[e]:t.value=this.settings[e])}),this.loadStreamingPreferences(),this.updateCustomSelectDisplays()}updateCustomSelectDisplays(){document.querySelectorAll(".custom-select-wrapper").forEach(e=>{const t=e._nativeSelect;if(t){const s=e.querySelector(".custom-select-trigger")?.querySelector(".custom-select-text");s&&(s.textContent=t.options[t.selectedIndex]?.text||"Select..."),e.querySelector(".custom-select-dropdown")?.querySelectorAll(".custom-select-option").forEach((i,a)=>{a===t.selectedIndex?i.classList.add("selected"):i.classList.remove("selected")})}})}async loadStreamingPreferences(){try{const e=f.currentProfileId;if(!e){console.log("No profile selected, using default transcoding settings");return}const t=await d.getSettings(),o=`streamingPreferences_${e}`;if(t.settings&&t.settings[o]){const s=typeof t.settings[o]=="string"?JSON.parse(t.settings[o]):t.settings[o];console.log("Loaded streaming preferences:",s);const n=document.getElementById("transcoding-mode");n&&(n.value=s.transcodingMode||"direct-play",this.settings["transcoding-mode"]=n.value);const i=document.getElementById("use-hardware-accel");i&&(i.checked=s.useHardwareAccel!==!1,this.settings["use-hardware-accel"]=i.checked);const a=document.getElementById("transcode-preset");a&&(a.value=s.preset||"p4",this.settings["transcode-preset"]=a.value);const r=document.getElementById("audio-transcoding");r&&(r.checked=s.audioTranscoding!==!1,this.settings["audio-transcoding"]=r.checked);const c=document.getElementById("video-transcoding");c&&(c.checked=s.videoTranscoding!==!1,this.settings["video-transcoding"]=c.checked),this.updateCustomSelectDisplays()}else console.log("No saved streaming preferences found for profile",e)}catch(e){console.error("Failed to load streaming preferences:",e)}}renderProfiles(){const e=document.querySelector(".profiles-grid");if(!e)return;e.querySelectorAll(".profile-card:not(.add-profile)").forEach(o=>o.remove()),this.profiles.forEach(o=>{const s=document.createElement("div");s.className="profile-card",s.dataset.profileId=o.id,s.innerHTML=`
        <div class="profile-card-avatar" style="background: linear-gradient(135deg, ${o.avatarColorPrimary}, ${o.avatarColorSecondary})"></div>
        <div class="profile-card-name">${o.name}</div>
        <button class="profile-card-btn">Edit</button>
      `;const n=e.querySelector(".add-profile");n?e.insertBefore(s,n):e.appendChild(s)}),this.setupProfiles()}setupNavigation(){document.querySelectorAll(".settings-nav-item").forEach(t=>{t.addEventListener("click",()=>{this.switchToSection(t.dataset.section)})})}switchToSection(e){const t=document.querySelectorAll(".settings-nav-item"),o=document.querySelectorAll(".settings-section");t.forEach(i=>i.classList.remove("active"));const s=document.querySelector(`[data-section="${e}"]`);s&&s.classList.add("active"),o.forEach(i=>i.classList.remove("active"));const n=document.getElementById(e);n&&n.classList.add("active"),this.focusedContentIndex=0,window.scrollTo({top:0,behavior:"smooth"})}initializeCustomSelects(){document.querySelectorAll(".settings-select").forEach(t=>{if(t.parentElement?.classList.contains("custom-select-wrapper"))return;const o=document.createElement("div");o.className="custom-select-wrapper";const s=document.createElement("div");s.className="custom-select-trigger";const n=document.createElement("span");n.className="custom-select-text",n.textContent=t.options?.[t.selectedIndex]?.text||"Select...";const i=document.createElementNS("http://www.w3.org/2000/svg","svg");i.setAttribute("class","custom-select-arrow"),i.setAttribute("viewBox","0 0 24 24");const a=document.createElementNS("http://www.w3.org/2000/svg","path");a.setAttribute("d","M7 10l5 5 5-5z"),i.appendChild(a),s.appendChild(n),s.appendChild(i);const r=document.createElement("div");r.className="custom-select-dropdown",Array.from(t.options).forEach((c,u)=>{const l=document.createElement("button");l.className="custom-select-option",l.textContent=c.text,l.dataset.value=c.value,l.dataset.index=u,u===t.selectedIndex&&l.classList.add("selected"),l.addEventListener("click",m=>{m.stopPropagation(),this.selectOption(o,t,l,u)}),r.appendChild(l)}),o.appendChild(s),o.appendChild(r),t.parentNode.insertBefore(o,t),s.addEventListener("click",c=>{c.stopPropagation(),this.toggleDropdown(o)}),o._nativeSelect=t})}toggleDropdown(e){const t=e.querySelector(".custom-select-trigger"),o=e.querySelector(".custom-select-dropdown"),s=t.classList.contains("active");if(this.closeAllDropdowns(),!s){e.classList.add("active"),t.classList.add("active"),o.classList.add("active"),this.currentSelectElement=e,this.selectMode=!0,this.selectOptionIndex=0;const n=o.querySelector(".custom-select-option.selected");n&&(this.selectOptionIndex=parseInt(n.dataset.index),this.updateDropdownFocus(o))}}closeAllDropdowns(){document.querySelectorAll(".custom-select-wrapper").forEach(e=>{e.classList.remove("active")}),document.querySelectorAll(".custom-select-trigger").forEach(e=>{e.classList.remove("active")}),document.querySelectorAll(".custom-select-dropdown").forEach(e=>{e.classList.remove("active")}),this.selectMode=!1,this.currentSelectElement=null}async selectOption(e,t,o,s){const i=e.querySelector(".custom-select-trigger").querySelector(".custom-select-text");e.querySelector(".custom-select-dropdown").querySelectorAll(".custom-select-option").forEach(u=>{u.classList.remove("selected")}),o.classList.add("selected"),i.textContent=o.textContent,t.selectedIndex=s,t.dispatchEvent(new Event("change"));const r=t.id,c=t.value;console.log(`${r} changed to:`,c),this.settings[r]=c,await this.saveSettings(),this.closeAllDropdowns()}updateDropdownFocus(e){const t=e.querySelectorAll(".custom-select-option");t.forEach(o=>o.classList.remove("focused")),t[this.selectOptionIndex]&&(t[this.selectOptionIndex].classList.add("focused"),t[this.selectOptionIndex].scrollIntoView({behavior:"smooth",block:"nearest"}))}setupToggles(){document.querySelectorAll(".settings-toggle input").forEach(s=>{s.addEventListener("change",async n=>{const i=n.target.id,a=n.target.checked;console.log(`Toggle ${i} changed to:`,a),i==="audio-transcoding"||i==="video-transcoding"||i==="use-hardware-accel"?await this.saveStreamingPreferences():(this.settings[i]=a,await this.saveSettings())})});const t=document.getElementById("transcoding-mode");t&&(t.addEventListener("change",async()=>{this.handleTranscodingModeChange(),await this.saveStreamingPreferences()}),this.handleTranscodingModeChange());const o=document.getElementById("transcode-preset");o&&o.addEventListener("change",async()=>{await this.saveStreamingPreferences()})}handleTranscodingModeChange(){const e=document.getElementById("transcoding-mode")?.value,t=document.getElementById("custom-mode-warning"),o=document.getElementById("audio-transcoding"),s=document.getElementById("video-transcoding");e==="custom"?(t&&(t.style.display="flex"),o&&(o.disabled=!1),s&&(s.disabled=!1)):(t&&(t.style.display="none"),o&&(o.disabled=!0),s&&(s.disabled=!0))}async saveStreamingPreferences(){try{const e=f.currentProfileId;if(!e){console.error("No profile selected, cannot save streaming preferences"),alert("Please select a profile first");return}const t=document.getElementById("transcoding-mode")?.value??"direct-play",o=document.getElementById("audio-transcoding")?.checked??!0,s=document.getElementById("video-transcoding")?.checked??!0,n=document.getElementById("use-hardware-accel")?.checked??!0,i=document.getElementById("transcode-preset")?.value??"p4",a={transcodingMode:t,audioTranscoding:o,videoTranscoding:s,useHardwareAccel:n,preset:i};console.log("Saving streaming preferences for profile",e,":",a),await d.updateStreamingPreferences(e,a),this.settings["transcoding-mode"]=t,this.settings["audio-transcoding"]=o,this.settings["video-transcoding"]=s,this.settings["use-hardware-accel"]=n,this.settings["transcode-preset"]=i,console.log("✅ Streaming preferences saved successfully"),this.showSaveNotification("Transcoding settings saved!")}catch(e){console.error("Failed to save streaming preferences:",e),alert("Failed to save streaming preferences. Please try again.")}}showSaveNotification(e){let t=document.getElementById("settings-save-notification");t||(t=document.createElement("div"),t.id="settings-save-notification",t.style.cssText=`
        position: fixed;
        top: 80px;
        right: 20px;
        background: #4caf50;
        color: white;
        padding: 12px 24px;
        border-radius: 8px;
        box-shadow: 0 4px 12px rgba(0,0,0,0.3);
        z-index: 10000;
        font-size: 14px;
        font-weight: 500;
        opacity: 0;
        transition: opacity 0.3s;
      `,document.body.appendChild(t)),t.textContent=e,t.style.opacity="1",setTimeout(()=>{t.style.opacity="0"},2e3)}setupModals(){document.getElementById("cancel-profile")?.addEventListener("click",()=>this.closeModal()),document.getElementById("cancel-add-profile")?.addEventListener("click",()=>this.closeModal()),document.getElementById("save-profile")?.addEventListener("click",async()=>{const e=document.getElementById("profile-name").value,t=this.currentProfileCard?.dataset.profileId;if(!e||!t||!this.selectedColor){alert("Please fill in all fields");return}const[o,s]=this.selectedColor.split(",");await this.updateExistingProfile(parseInt(t),{name:e,avatarColorPrimary:o,avatarColorSecondary:s})}),document.getElementById("create-profile")?.addEventListener("click",async()=>{const e=document.getElementById("new-profile-name").value;if(!e||!this.selectedColor){alert("Please fill in all fields");return}const[t,o]=this.selectedColor.split(",");await this.createNewProfile(e,t,o)}),document.querySelectorAll(".modal-overlay").forEach(e=>{e.addEventListener("click",t=>{t.target===e&&this.closeModal()})}),document.querySelectorAll(".modal-close").forEach(e=>{e.addEventListener("click",()=>this.closeModal())}),document.querySelectorAll(".color-option").forEach(e=>{e.addEventListener("click",()=>{this.selectColor(e)})})}setupProfiles(){document.querySelectorAll(".profile-card").forEach(e=>{e.addEventListener("click",()=>{e.classList.contains("add-profile")?this.showAddProfileModal():this.showEditProfileModal(e)})}),document.querySelectorAll(".profile-card-btn").forEach(e=>{e.addEventListener("click",t=>{t.stopPropagation();const o=e.closest(".profile-card");o&&this.showEditProfileModal(o)})})}showAddProfileModal(){const e=document.getElementById("add-profile-modal");e.classList.add("active"),this.modalActive=!0,this.modalFocusIndex=0,this.selectedColor=null,document.getElementById("new-profile-name").value="";const t=e.querySelector(".color-option");t&&this.selectColor(t),this.updateModalFocus()}showEditProfileModal(e){const t=document.getElementById("edit-profile-modal"),o=e.querySelector(".profile-card-name").textContent;t.classList.add("active"),this.modalActive=!0,this.modalFocusIndex=0,this.currentProfileCard=e,document.getElementById("profile-name").value=o;const s=t.querySelector("[data-color]");s&&this.selectColor(s),this.updateModalFocus()}closeModal(){document.querySelectorAll(".modal-overlay").forEach(t=>t.classList.remove("active")),this.modalActive=!1,this.modalFocusIndex=0,this.selectedColor=null,this.currentProfileCard=null}selectColor(e){e.closest(".modal-overlay").querySelectorAll(".color-option").forEach(o=>o.classList.remove("selected")),e.classList.add("selected"),this.selectedColor=e.dataset.color}getModalInteractiveElements(){const e=document.querySelector(".modal-overlay.active");if(!e)return[];const t=[],o=e.querySelector(".modal-input");o&&t.push(o),e.querySelectorAll(".color-option").forEach(n=>t.push(n)),e.querySelectorAll(".modal-btn").forEach(n=>t.push(n));const s=e.querySelector(".modal-close");return s&&t.push(s),t}updateModalFocus(){const e=this.getModalInteractiveElements();e.forEach(t=>t.classList.remove("focused")),e[this.modalFocusIndex]&&(e[this.modalFocusIndex].classList.add("focused"),e[this.modalFocusIndex].scrollIntoView({behavior:"smooth",block:"nearest"}))}getInteractiveElements(e){if(!e)return[];const t=[];return e.querySelectorAll(".settings-item").forEach(s=>{const n=s.querySelector(".custom-select-wrapper"),i=s.querySelector(".settings-toggle");n?t.push(n):i&&t.push(i)}),e.querySelectorAll(".settings-link-btn, .profile-card-btn, .device-remove, .profile-card").forEach(s=>{t.includes(s)||t.push(s)}),t}handleKeyboard(e){const t=Array.from(document.querySelectorAll(".settings-nav-item")),o=document.querySelector(".settings-section.active");if(this.modalActive){const s=this.getModalInteractiveElements();if(e.key==="ArrowDown"||e.key==="ArrowRight")e.preventDefault(),this.modalFocusIndex=(this.modalFocusIndex+1)%s.length,this.updateModalFocus();else if(e.key==="ArrowUp"||e.key==="ArrowLeft")e.preventDefault(),this.modalFocusIndex=(this.modalFocusIndex-1+s.length)%s.length,this.updateModalFocus();else if(e.key==="Enter"){e.preventDefault();const n=s[this.modalFocusIndex];n&&(n.classList.contains("color-option")?this.selectColor(n):n.classList.contains("modal-close")?this.closeModal():n.click())}else e.key==="Escape"&&(e.preventDefault(),this.closeModal());return}if(this.selectMode&&this.currentSelectElement){const s=this.currentSelectElement.querySelector(".custom-select-dropdown"),n=s.querySelectorAll(".custom-select-option");if(e.key==="ArrowDown")e.preventDefault(),this.selectOptionIndex=Math.min(this.selectOptionIndex+1,n.length-1),this.updateDropdownFocus(s);else if(e.key==="ArrowUp")e.preventDefault(),this.selectOptionIndex=Math.max(this.selectOptionIndex-1,0),this.updateDropdownFocus(s);else if(e.key==="Enter"){e.preventDefault();const i=n[this.selectOptionIndex];if(i){const a=this.currentSelectElement._nativeSelect;this.selectOption(this.currentSelectElement,a,i,this.selectOptionIndex)}}else e.key==="Escape"&&(e.preventDefault(),this.closeAllDropdowns());return}if(this.focusedArea==="back")e.key==="ArrowDown"?(e.preventDefault(),this.focusedArea="nav",this.focusedNavIndex=0,this.updateFocus()):e.key==="ArrowRight"?(e.preventDefault(),this.focusedArea="content",this.focusedContentIndex=0,this.updateFocus()):e.key==="Enter"&&(e.preventDefault(),window.location.href="index.html");else if(this.focusedArea==="nav")e.key==="ArrowDown"?(e.preventDefault(),this.focusedNavIndex=(this.focusedNavIndex+1)%t.length,this.updateFocus()):e.key==="ArrowUp"?(e.preventDefault(),this.focusedNavIndex===0?(this.focusedArea="back",this.updateFocus()):(this.focusedNavIndex=(this.focusedNavIndex-1+t.length)%t.length,this.updateFocus())):e.key==="ArrowRight"?(e.preventDefault(),this.focusedArea="content",this.focusedContentIndex=0,this.updateFocus()):e.key==="Enter"&&(e.preventDefault(),t[this.focusedNavIndex].click());else if(this.focusedArea==="content"){const s=this.getInteractiveElements(o);if(e.key==="ArrowDown")e.preventDefault(),this.focusedContentIndex=Math.min(this.focusedContentIndex+1,s.length-1),this.updateFocus(),this.scrollToFocusedElement(s[this.focusedContentIndex]);else if(e.key==="ArrowUp")e.preventDefault(),this.focusedContentIndex=Math.max(this.focusedContentIndex-1,0),this.updateFocus(),this.scrollToFocusedElement(s[this.focusedContentIndex]);else if(e.key==="ArrowLeft")e.preventDefault(),this.focusedArea="nav",this.updateFocus();else if(e.key==="Enter"||e.key===" "){e.preventDefault();const n=s[this.focusedContentIndex];if(n)if(n.classList.contains("settings-toggle")){const i=n.querySelector('input[type="checkbox"]');i&&(i.checked=!i.checked,i.dispatchEvent(new Event("change")))}else n.classList.contains("custom-select-wrapper")?this.toggleDropdown(n):n.tagName==="BUTTON"?n.click():n.classList.contains("profile-card")&&(n.classList.contains("add-profile")?this.showAddProfileModal():n.querySelector(".profile-card-btn")&&this.showEditProfileModal(n))}}}updateFocus(){const e=Array.from(document.querySelectorAll(".settings-nav-item")),t=document.querySelector(".settings-section.active"),o=document.querySelector(".back-btn");if(e.forEach(s=>s.classList.remove("focused")),o&&o.classList.remove("focused"),document.querySelectorAll(".settings-group").forEach(s=>{s.style.zIndex=""}),t){const s=this.getInteractiveElements(t);if(s.forEach(n=>n.classList.remove("focused")),this.focusedArea==="content"&&s[this.focusedContentIndex]){s[this.focusedContentIndex].classList.add("focused");const n=s[this.focusedContentIndex].closest(".settings-group");n&&(n.style.zIndex="100")}}this.focusedArea==="back"&&o?o.classList.add("focused"):this.focusedArea==="nav"&&e[this.focusedNavIndex].classList.add("focused")}scrollToFocusedElement(e){e&&e.scrollIntoView({behavior:"smooth",block:"center"})}async saveSettings(){try{await d.updateSettings(this.settings),console.log("Settings saved successfully")}catch(e){console.error("Failed to save settings:",e),alert("Failed to save settings. Please try again.")}}async createNewProfile(e,t,o){try{await d.createProfile(e,t,o),await this.loadProfiles(),this.closeModal(),alert("Profile created successfully!")}catch(s){console.error("Failed to create profile:",s),alert("Failed to create profile. Please try again.")}}async updateExistingProfile(e,t){try{await d.updateProfile(e,t),await this.loadProfiles(),this.closeModal(),alert("Profile updated successfully!")}catch(o){console.error("Failed to update profile:",o),alert("Failed to update profile. Please try again.")}}async deleteExistingProfile(e){if(confirm("Are you sure you want to delete this profile?"))try{await d.deleteProfile(e),await this.loadProfiles(),alert("Profile deleted successfully!")}catch(t){console.error("Failed to delete profile:",t),alert("Failed to delete profile. Please try again.")}}}class v{constructor(){this.currentVersion="1.2.6",this.updateCheckUrl="https://api.github.com/repos/JarlTheGamer/Applications./releases/latest",this.checkInterval=24*60*60*1e3,this.lastCheckKey="lanflix_last_update_check",this.skipVersionKey="lanflix_skip_version"}async initialize(){await this.loadCurrentVersion(),this.shouldCheckForUpdates()&&await this.checkForUpdates(!1),this.startPeriodicChecks()}async loadCurrentVersion(){try{const e=document.querySelector('meta[name="app-version"]');e&&(this.currentVersion=e.content)}catch(e){console.warn("Could not load app version:",e)}}shouldCheckForUpdates(){const e=localStorage.getItem(this.lastCheckKey);if(!e)return!0;const t=parseInt(e,10);return Date.now()-t>this.checkInterval}startPeriodicChecks(){setInterval(()=>{this.checkForUpdates(!1)},this.checkInterval)}async checkForUpdates(e=!0){try{console.log("🔍 Checking for updates...");const t=await fetch(this.updateCheckUrl,{headers:{Accept:"application/vnd.github.v3+json"}});if(!t.ok)throw new Error("Failed to check for updates");const o=await t.json(),s=o.tag_name.replace("v",""),n=this.currentVersion.replace("v","");if(localStorage.setItem(this.lastCheckKey,Date.now().toString()),localStorage.getItem(this.skipVersionKey)===s)return console.log("User skipped this version"),null;if(this.isNewerVersion(s,n)){console.log(`✨ Update available: ${s} (current: ${n})`);const a={version:s,currentVersion:n,releaseNotes:o.body||"No release notes available",publishedAt:o.published_at,downloadUrl:this.getDownloadUrl(o),htmlUrl:o.html_url};return this.showUpdateNotification(a),a}else return console.log("✅ App is up to date"),e&&this.showNoUpdateMessage(),null}catch(t){return console.error("Failed to check for updates:",t),e&&this.showErrorMessage("Failed to check for updates. Please try again later."),null}}isNewerVersion(e,t){const o=e.split(".").map(Number),s=t.split(".").map(Number);for(let n=0;n<Math.max(o.length,s.length);n++){const i=o[n]||0,a=s[n]||0;if(i>a)return!0;if(i<a)return!1}return!1}getDownloadUrl(e){const t=e.assets||[],o=/android/i.test(navigator.userAgent),s=/windows/i.test(navigator.userAgent),n=/mac/i.test(navigator.userAgent),i=/linux/i.test(navigator.userAgent)&&!o;if(o){const a=t.find(r=>r.name.endsWith(".apk"));if(a)return a.browser_download_url}if(s){const a=t.find(r=>r.name.endsWith(".exe")||r.name.includes("windows"));if(a)return a.browser_download_url}if(n){const a=t.find(r=>r.name.endsWith(".dmg")||r.name.includes("mac"));if(a)return a.browser_download_url}if(i){const a=t.find(r=>r.name.endsWith(".AppImage")||r.name.includes("linux"));if(a)return a.browser_download_url}return e.html_url}showUpdateNotification(e){this.hideUpdateNotification();const t=document.createElement("div");t.id="update-notification-modal",t.style.cssText=`
      position: fixed;
      top: 0;
      left: 0;
      right: 0;
      bottom: 0;
      background: rgba(0, 0, 0, 0.85);
      display: flex;
      align-items: center;
      justify-content: center;
      z-index: 10000;
      animation: fadeIn 0.3s ease-out;
    `;const o=this.formatReleaseNotes(e.releaseNotes);t.innerHTML=`
      <div style="
        background: linear-gradient(135deg, #1a1a1a 0%, #2d2d2d 100%);
        border-radius: 12px;
        padding: 40px;
        max-width: 600px;
        width: 90%;
        max-height: 80vh;
        overflow-y: auto;
        box-shadow: 0 20px 60px rgba(0, 0, 0, 0.5);
      ">
        <div style="text-align: center; margin-bottom: 30px;">
          <div style="font-size: 48px; margin-bottom: 10px;">🎉</div>
          <h2 style="color: #fff; font-size: 28px; margin: 0 0 10px 0;">Update Available!</h2>
          <p style="color: #999; font-size: 16px; margin: 0;">
            Version ${e.version} is now available
          </p>
          <p style="color: #666; font-size: 14px; margin: 5px 0 0 0;">
            Current version: ${e.currentVersion}
          </p>
        </div>

        <div style="
          background: rgba(255, 255, 255, 0.05);
          border-radius: 8px;
          padding: 20px;
          margin-bottom: 30px;
          max-height: 300px;
          overflow-y: auto;
        ">
          <h3 style="color: #fff; font-size: 18px; margin: 0 0 15px 0;">What's New:</h3>
          <div style="color: #ccc; font-size: 14px; line-height: 1.6;">
            ${o}
          </div>
        </div>

        <div style="display: flex; gap: 12px; justify-content: center; flex-wrap: wrap;">
          <button id="update-now-btn" style="
            background: #e50914;
            color: white;
            border: none;
            padding: 14px 32px;
            font-size: 16px;
            font-weight: 600;
            border-radius: 6px;
            cursor: pointer;
            transition: all 0.2s;
            flex: 1;
            min-width: 140px;
          " onmouseover="this.style.background='#f40612'; this.style.transform='scale(1.05)'" 
             onmouseout="this.style.background='#e50914'; this.style.transform='scale(1)'">
            Update Now
          </button>
          <button id="update-later-btn" style="
            background: rgba(255, 255, 255, 0.1);
            color: white;
            border: 1px solid rgba(255, 255, 255, 0.2);
            padding: 14px 32px;
            font-size: 16px;
            font-weight: 600;
            border-radius: 6px;
            cursor: pointer;
            transition: all 0.2s;
            flex: 1;
            min-width: 140px;
          " onmouseover="this.style.background='rgba(255, 255, 255, 0.15)'" 
             onmouseout="this.style.background='rgba(255, 255, 255, 0.1)'">
            Later
          </button>
          <button id="update-skip-btn" style="
            background: transparent;
            color: #999;
            border: none;
            padding: 14px 20px;
            font-size: 14px;
            cursor: pointer;
            transition: color 0.2s;
          " onmouseover="this.style.color='#fff'" 
             onmouseout="this.style.color='#999'">
            Skip This Version
          </button>
        </div>
      </div>
    `,document.body.appendChild(t);const s=document.createElement("style");s.textContent=`
      @keyframes fadeIn {
        from { opacity: 0; }
        to { opacity: 1; }
      }
    `,document.head.appendChild(s),document.getElementById("update-now-btn").addEventListener("click",()=>{this.startUpdate(e)}),document.getElementById("update-later-btn").addEventListener("click",()=>{this.hideUpdateNotification()}),document.getElementById("update-skip-btn").addEventListener("click",()=>{localStorage.setItem(this.skipVersionKey,e.version),this.hideUpdateNotification()})}formatReleaseNotes(e){if(!e)return"<p>No release notes available.</p>";let t=e.replace(/^### (.+)$/gm,'<h4 style="color: #fff; margin: 15px 0 10px 0;">$1</h4>').replace(/^## (.+)$/gm,'<h3 style="color: #fff; margin: 20px 0 10px 0;">$1</h3>').replace(/^- (.+)$/gm,'<li style="margin: 5px 0;">$1</li>').replace(/\*\*(.+?)\*\*/g,"<strong>$1</strong>").replace(/\n\n/g,'</p><p style="margin: 10px 0;">').replace(/^(?!<[hl]|<li)/gm,'<p style="margin: 10px 0;">');return t=t.replace(/(<li[^>]*>.*<\/li>)/s,'<ul style="margin: 10px 0; padding-left: 20px;">$1</ul>'),t}hideUpdateNotification(){const e=document.getElementById("update-notification-modal");e&&(e.style.animation="fadeOut 0.3s ease-out",setTimeout(()=>e.remove(),300))}async startUpdate(e){const t=/android/i.test(navigator.userAgent),o=window.Capacitor!==void 0;t&&o?await this.updateAndroidApp(e):this.openDownloadPage(e)}async updateAndroidApp(e){try{this.showUpdateProgress(),window.Capacitor&&window.Capacitor.Plugins.Browser?await window.Capacitor.Plugins.Browser.open({url:e.downloadUrl}):window.open(e.downloadUrl,"_blank"),this.hideUpdateProgress(),this.showUpdateInstructions()}catch(t){console.error("Failed to update Android app:",t),this.hideUpdateProgress(),this.showErrorMessage("Failed to start update. Please download manually from the website.")}}openDownloadPage(e){window.open(e.downloadUrl,"_blank"),this.hideUpdateNotification(),setTimeout(()=>{this.showUpdateInstructions()},500)}showUpdateProgress(){const e=document.createElement("div");e.id="update-progress-modal",e.style.cssText=`
      position: fixed;
      top: 0;
      left: 0;
      right: 0;
      bottom: 0;
      background: rgba(0, 0, 0, 0.9);
      display: flex;
      align-items: center;
      justify-content: center;
      z-index: 10001;
    `,e.innerHTML=`
      <div style="text-align: center; color: white;">
        <div style="font-size: 48px; margin-bottom: 20px;">⬇️</div>
        <h2 style="font-size: 24px; margin-bottom: 10px;">Downloading Update...</h2>
        <p style="color: #999;">Please wait while we download the latest version</p>
      </div>
    `,document.body.appendChild(e)}hideUpdateProgress(){const e=document.getElementById("update-progress-modal");e&&e.remove()}showUpdateInstructions(){const t=/android/i.test(navigator.userAgent)?"The APK file is downloading. Once complete, open it to install the update.":"The download has started. Once complete, install the update and restart the app.";this.showInfoMessage(t)}showNoUpdateMessage(){this.showInfoMessage("You're running the latest version!")}showInfoMessage(e){const t=document.createElement("div");t.style.cssText=`
      position: fixed;
      top: 80px;
      left: 50%;
      transform: translateX(-50%);
      background: rgba(255, 255, 255, 0.95);
      color: #000;
      padding: 16px 32px;
      border-radius: 8px;
      font-size: 16px;
      font-weight: 500;
      z-index: 10002;
      box-shadow: 0 4px 12px rgba(0, 0, 0, 0.3);
      animation: slideDown 0.3s ease-out;
    `,t.textContent=e,document.body.appendChild(t),setTimeout(()=>{t.style.animation="slideUp 0.3s ease-out",setTimeout(()=>t.remove(),300)},3e3)}showErrorMessage(e){const t=document.createElement("div");t.style.cssText=`
      position: fixed;
      top: 80px;
      left: 50%;
      transform: translateX(-50%);
      background: rgba(229, 9, 20, 0.95);
      color: white;
      padding: 16px 32px;
      border-radius: 8px;
      font-size: 16px;
      font-weight: 500;
      z-index: 10002;
      box-shadow: 0 4px 12px rgba(0, 0, 0, 0.3);
      animation: slideDown 0.3s ease-out;
    `,t.textContent=e,document.body.appendChild(t),setTimeout(()=>{t.style.animation="slideUp 0.3s ease-out",setTimeout(()=>t.remove(),300)},4e3)}async checkNow(){return await this.checkForUpdates(!0)}}const h=new v;document.addEventListener("DOMContentLoaded",async()=>{try{await new y().initialize(),await h.initialize();const e=document.getElementById("app-version");e&&(e.textContent=h.currentVersion);const t=document.getElementById("check-updates-btn");t&&t.addEventListener("click",async()=>{t.disabled=!0;const s=t.innerHTML;if(t.innerHTML=`
          <svg viewBox="0 0 24 24" width="20" height="20" style="margin-right: 8px; animation: spin 1s linear infinite;">
            <path fill="currentColor" d="M12 4V1L8 5l4 4V6c3.31 0 6 2.69 6 6 0 1.01-.25 1.97-.7 2.8l1.46 1.46C19.54 15.03 20 13.57 20 12c0-4.42-3.58-8-8-8zm0 14c-3.31 0-6-2.69-6-6 0-1.01.25-1.97.7-2.8L5.24 7.74C4.46 8.97 4 10.43 4 12c0 4.42 3.58 8 8 8v3l4-4-4-4v3z"/>
          </svg>
          Checking...
        `,!document.getElementById("spin-animation")){const n=document.createElement("style");n.id="spin-animation",n.textContent=`
            @keyframes spin {
              from { transform: rotate(0deg); }
              to { transform: rotate(360deg); }
            }
          `,document.head.appendChild(n)}await h.checkNow(),t.disabled=!1,t.innerHTML=s});const o=document.getElementById("auto-update-toggle");if(o){const s=localStorage.getItem("lanflix_auto_update")!=="false";o.checked=s,o.addEventListener("change",()=>{localStorage.setItem("lanflix_auto_update",o.checked.toString()),o.checked&&h.startPeriodicChecks()})}}catch(p){console.error("Failed to initialize settings:",p),alert("Failed to load settings. Please refresh the page.")}});

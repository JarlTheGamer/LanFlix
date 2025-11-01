import"./api-client-7IhkegDa.js";/* empty css             */import{t as o}from"./tv-navigation-BRt9J30j.js";import{P as r}from"./profile-manager-BH47FVgT.js";import{s as i}from"./data-DddsZ_ae.js";document.addEventListener("DOMContentLoaded",()=>{o.initialize()});document.addEventListener("DOMContentLoaded",async()=>{try{const e=new r,a=e.selectProfile.bind(e);e.selectProfile=function(t){this.selectedProfileId=t;const d=this.profiles.find(n=>n.id===t);i.currentProfileId=t,i.saveState(),window.location.href="index.html"},await e.initialize(),e.show(),document.addEventListener("keydown",t=>e.handleKeyboard(t))}catch(e){console.error("Failed to initialize profiles:",e),document.body.innerHTML=`
      <div style="display: flex; align-items: center; justify-content: center; height: 100vh; color: white; text-align: center; padding: 20px;">
        <div>
          <h1>Failed to load profiles</h1>
          <p>Please check your connection and try again.</p>
          <button onclick="location.reload()" style="margin-top: 20px; padding: 10px 20px; font-size: 16px; cursor: pointer;">
            Retry
          </button>
        </div>
      </div>
    `}});

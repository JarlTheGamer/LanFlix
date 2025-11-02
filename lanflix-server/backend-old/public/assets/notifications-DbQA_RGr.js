import{a as s}from"./api-client-7IhkegDa.js";/* empty css             *//* empty css                     */class l{constructor(){this.currentTab="notifications",this.notifications=[],this.downloads=[],this.jobs=[],this.refreshInterval=null}init(){this.setupTabs(),this.setupEventListeners(),this.loadNotifications(),this.loadJobs(),this.refreshInterval=setInterval(()=>{this.currentTab==="downloads"?this.loadDownloads():this.currentTab==="jobs"&&this.loadJobs()},5e3)}setupTabs(){document.querySelectorAll(".tab-btn").forEach(t=>{t.addEventListener("click",()=>{const i=t.dataset.tab;this.switchTab(i)})})}switchTab(e){this.currentTab=e,document.querySelectorAll(".tab-btn").forEach(t=>{t.classList.toggle("active",t.dataset.tab===e)}),document.querySelectorAll(".tab-content").forEach(t=>{t.classList.toggle("active",t.id===`${e}-tab`)}),e==="notifications"?this.loadNotifications():e==="downloads"?this.loadDownloads():e==="jobs"&&this.loadJobs()}setupEventListeners(){document.getElementById("clear-notifications")?.addEventListener("click",()=>{this.clearNotifications()}),document.getElementById("refresh-downloads")?.addEventListener("click",()=>{this.loadDownloads()}),document.getElementById("refresh-jobs")?.addEventListener("click",()=>{this.loadJobs()})}async loadNotifications(){try{const e=this.getCurrentProfileId(),t=await s.get(`/api/notifications/${e}`);this.notifications=t.notifications||[],this.renderNotifications(),this.updateBadge()}catch(e){console.error("Failed to load notifications:",e),this.renderEmptyState("notifications-list","Failed to load notifications")}}async loadDownloads(){try{this.downloads=[],this.renderDownloads()}catch(e){console.error("Failed to load downloads:",e),this.renderEmptyState("downloads-list","Failed to load downloads")}}async loadJobs(){try{const e=await s.get("/api/jobs/status");this.jobs=e.jobs||[],this.renderJobs()}catch(e){console.error("Failed to load jobs:",e),this.renderEmptyState("jobs-list","Failed to load jobs")}}renderNotifications(){const e=document.getElementById("notifications-list");if(!this.notifications.length){this.renderEmptyState("notifications-list","No notifications");return}e.innerHTML=this.notifications.map(t=>`
      <div class="notification-item" data-id="${t.id}">
        <div class="notification-header">
          <div class="notification-title">${t.contentTitle||"Content"}</div>
          <div class="notification-time">${this.formatTime(t.notificationSentAt)}</div>
        </div>
        <div class="notification-message">
          ${this.getNotificationMessage(t)}
        </div>
        ${t.userResponse?`
          <div class="notification-actions">
            <span class="status-badge ${t.userResponse==="keep"?"running":"error"}">
              ${t.userResponse==="keep"?"Kept":"Deleted"}
            </span>
          </div>
        `:`
          <div class="notification-actions">
            <button class="btn-action btn-keep" onclick="notificationsManager.respondToNotification(${t.id}, 'keep')">
              Keep Watching
            </button>
            <button class="btn-action btn-delete" onclick="notificationsManager.respondToNotification(${t.id}, 'delete')">
              Delete
            </button>
          </div>
        `}
      </div>
    `).join("")}renderDownloads(){const e=document.getElementById("downloads-list");if(!this.downloads.length){this.renderEmptyState("downloads-list","No active downloads");return}e.innerHTML=this.downloads.map(t=>`
      <div class="download-item" data-id="${t.id}">
        <div class="download-header">
          <div class="download-title">${t.title}</div>
          <div class="download-status">${t.status}</div>
        </div>
        <div class="download-info">
          ${t.progress}% complete • ${t.speed||"Calculating..."} • ETA: ${t.eta||"Unknown"}
        </div>
        <div class="progress-bar">
          <div class="progress-fill" style="width: ${t.progress}%"></div>
        </div>
        ${t.status==="downloading"?`
          <div class="download-actions">
            <button class="btn-action btn-cancel" onclick="notificationsManager.cancelDownload(${t.id})">
              Cancel
            </button>
          </div>
        `:""}
      </div>
    `).join("")}renderJobs(){const e=document.getElementById("jobs-list");if(!this.jobs.length){this.renderEmptyState("jobs-list","No jobs configured");return}e.innerHTML=this.jobs.map(t=>`
      <div class="job-item" data-name="${t.name}">
        <div class="job-header">
          <div class="job-title">${this.formatJobName(t.name)}</div>
          <span class="status-badge ${t.running?"running":"idle"}">
            ${t.running?"Running":"Idle"}
          </span>
        </div>
        <div class="job-info">
          ${t.description||"Background job"}
        </div>
        <div class="job-schedule">
          Schedule: ${t.schedule||"Manual"} • Last run: ${t.lastRun?this.formatTime(t.lastRun):"Never"}
        </div>
        <div class="job-actions">
          <button class="btn-trigger" onclick="notificationsManager.triggerJob('${t.name}')" ${t.running?"disabled":""}>
            Trigger Now
          </button>
        </div>
      </div>
    `).join("")}renderEmptyState(e,t){const i=document.getElementById(e),o=this.getEmptyStateIcon(e);i.innerHTML=`
      <div class="empty-state">
        ${o}
        <p>${t}</p>
      </div>
    `}getEmptyStateIcon(e){return e==="notifications-list"?`<svg viewBox="0 0 24 24" aria-hidden="true">
        <path d="M12 22c1.1 0 2-.9 2-2h-4c0 1.1.9 2 2 2zm6-6v-5c0-3.07-1.63-5.64-4.5-6.32V4c0-.83-.67-1.5-1.5-1.5s-1.5.67-1.5 1.5v.68C7.64 5.36 6 7.92 6 11v5l-2 2v1h16v-1l-2-2z" />
      </svg>`:e==="downloads-list"?`<svg viewBox="0 0 24 24" aria-hidden="true">
        <path d="M19 9h-4V3H9v6H5l7 7 7-7zM5 18v2h14v-2H5z" />
      </svg>`:`<svg viewBox="0 0 24 24" aria-hidden="true">
        <path d="M12 2C6.48 2 2 6.48 2 12s4.48 10 10 10 10-4.48 10-10S17.52 2 12 2zm-2 15l-5-5 1.41-1.41L10 14.17l7.59-7.59L19 8l-9 9z" />
      </svg>`}async respondToNotification(e,t){try{await s.post(`/api/notifications/${e}/respond`,{response:t}),await this.loadNotifications()}catch(i){console.error("Failed to respond to notification:",i),alert("Failed to respond to notification")}}async cancelDownload(e){try{console.log("Cancelling download:",e),await this.loadDownloads()}catch(t){console.error("Failed to cancel download:",t),alert("Failed to cancel download")}}async triggerJob(e){try{await s.post(`/api/jobs/${e}/trigger`),alert(`Job "${this.formatJobName(e)}" triggered successfully`),await this.loadJobs()}catch(t){console.error("Failed to trigger job:",t),alert("Failed to trigger job")}}async clearNotifications(){if(confirm("Clear all notifications?"))try{this.notifications=[],this.renderNotifications(),this.updateBadge()}catch(e){console.error("Failed to clear notifications:",e),alert("Failed to clear notifications")}}updateBadge(){const e=document.getElementById("notification-badge"),t=this.notifications.filter(i=>!i.userResponse).length;e&&(t>0?(e.textContent=t>99?"99+":t,e.classList.add("active")):e.classList.remove("active"))}getNotificationMessage(e){return e.type==="keep_watching"?`This content is scheduled for deletion on ${new Date(e.scheduledDeleteAt).toLocaleDateString()}. Would you like to keep watching?`:e.message||"Notification"}formatJobName(e){return e.split("-").map(t=>t.charAt(0).toUpperCase()+t.slice(1)).join(" ")}formatTime(e){if(!e)return"Unknown";const t=new Date(e),o=new Date-t,n=Math.floor(o/6e4),r=Math.floor(o/36e5),d=Math.floor(o/864e5);return n<1?"Just now":n<60?`${n}m ago`:r<24?`${r}h ago`:d<7?`${d}d ago`:t.toLocaleDateString()}getCurrentProfileId(){return parseInt(localStorage.getItem("selectedProfileId")||"1",10)}destroy(){this.refreshInterval&&clearInterval(this.refreshInterval)}}const a=new l;window.notificationsManager=a;document.addEventListener("DOMContentLoaded",()=>{a.init()});window.addEventListener("beforeunload",()=>{a.destroy()});

/**
 * App & Server Unified OTA Updater Module
 * Modern 🎉 OTA Update Experience with Confetti & Progress Tracking
 */

export class AppUpdater {
    constructor() {
        this.currentVersion = '1.4.1';
        this.progressInterval = null;
        this.isUpdating = false;
        this.lastCheckKey = 'lanflix_last_update_check';
        this.skipVersionKey = 'lanflix_skip_version';
    }

    async initialize() {
        await this.loadCurrentVersion();
    }

    async loadCurrentVersion() {
        try {
            const response = await fetch('/api/server-update/version', { cache: 'no-store' });
            if (response.ok) {
                const data = await response.json();
                if (data && data.version) {
                    this.currentVersion = data.version;
                }
            }
        } catch (error) {
            console.warn('Could not load server version:', error);
        }
    }

    async checkForUpdates(userInitiated = false) {
        const checkBtn = document.getElementById('check-updates-btn');
        const originalHtml = checkBtn ? checkBtn.innerHTML : '';

        try {
            if (checkBtn && userInitiated) {
                checkBtn.disabled = true;
                checkBtn.innerHTML = `
                    <svg viewBox="0 0 24 24" width="18" height="18" style="animation: spin 1s linear infinite; margin-right: 6px;">
                        <path fill="currentColor" d="M12 4V1L8 5l4 4V6c3.31 0 6 2.69 6 6 0 1.01-.25 1.97-.7 2.8l1.46 1.46C19.54 15.03 20 13.57 20 12c0-4.42-3.58-8-8-8z"/>
                    </svg> Checking for updates...`;
            }

            const response = await fetch('/api/server-update/check', { cache: 'no-store' });
            const data = await response.json();

            if (checkBtn && userInitiated) {
                checkBtn.disabled = false;
                checkBtn.innerHTML = originalHtml;
            }

            if (data.updateAvailable) {
                const updateInfo = {
                    version: data.latestVersion,
                    currentVersion: data.currentVersion || this.currentVersion,
                    releaseNotes: data.releaseNotes,
                    downloadUrl: data.downloadUrl,
                    fileSize: data.fileSize
                };

                const skippedVersion = localStorage.getItem(this.skipVersionKey);
                if (!userInitiated && skippedVersion === updateInfo.version) {
                    return false;
                }

                this.showUpdateNotification(updateInfo);
                return true;
            } else {
                if (userInitiated) {
                    this.showNoUpdateMessage(`Lanflix is up to date! (Version ${data.currentVersion || this.currentVersion})`);
                }
                return false;
            }
        } catch (error) {
            console.error('Failed to check for updates:', error);
            if (checkBtn && userInitiated) {
                checkBtn.disabled = false;
                checkBtn.innerHTML = originalHtml;
            }
            if (userInitiated) {
                this.showErrorMessage('Could not check for updates. Check server internet connection.');
            }
            return false;
        }
    }

    /**
     * Launch 🎉 Confetti Burst Animation
     */
    triggerConfetti() {
        try {
            const canvas = document.createElement('canvas');
            canvas.id = 'ota-confetti-canvas';
            canvas.style.cssText = `
                position: fixed;
                top: 0;
                left: 0;
                width: 100vw;
                height: 100vh;
                pointer-events: none;
                z-index: 10005;
            `;
            document.body.appendChild(canvas);

            const ctx = canvas.getContext('2d');
            canvas.width = window.innerWidth;
            canvas.height = window.innerHeight;

            const particles = [];
            const colors = ['#e50914', '#ffffff', '#ffd700', '#00e5ff', '#ff4081', '#76ff03'];

            for (let i = 0; i < 90; i++) {
                particles.push({
                    x: canvas.width / 2,
                    y: canvas.height / 3,
                    vx: (Math.random() - 0.5) * 14,
                    vy: (Math.random() - 0.7) * 16,
                    size: Math.random() * 8 + 4,
                    color: colors[Math.floor(Math.random() * colors.length)],
                    rotation: Math.random() * 360,
                    rSpeed: (Math.random() - 0.5) * 10,
                    opacity: 1
                });
            }

            let animationFrame;
            const render = () => {
                ctx.clearRect(0, 0, canvas.width, canvas.height);
                let alive = false;

                particles.forEach(p => {
                    p.x += p.vx;
                    p.y += p.vy;
                    p.vy += 0.35; // Gravity
                    p.opacity -= 0.012;
                    p.rotation += p.rSpeed;

                    if (p.opacity > 0) {
                        alive = true;
                        ctx.save();
                        ctx.globalAlpha = Math.max(0, p.opacity);
                        ctx.translate(p.x, p.y);
                        ctx.rotate((p.rotation * Math.PI) / 180);
                        ctx.fillStyle = p.color;
                        ctx.fillRect(-p.size / 2, -p.size / 2, p.size, p.size);
                        ctx.restore();
                    }
                });

                if (alive) {
                    animationFrame = requestAnimationFrame(render);
                } else {
                    canvas.remove();
                }
            };

            render();
        } catch (e) {
            console.warn('Confetti animation error:', e);
        }
    }

    /**
     * Show modern 🎉 "Update Available!" notification modal
     */
    showUpdateNotification(updateInfo) {
        this.hideUpdateNotification();
        this.triggerConfetti();

        const modal = document.createElement('div');
        modal.id = 'update-notification-modal';
        modal.className = 'modal-overlay active';
        modal.style.cssText = `
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
            backdrop-filter: blur(8px);
        `;

        const releaseNotesHtml = this.formatReleaseNotes(updateInfo.releaseNotes);
        const sizeText = updateInfo.fileSize ? ` • ${Math.round(updateInfo.fileSize / (1024 * 1024))} MB` : '';

        modal.innerHTML = `
            <div style="
                background: linear-gradient(135deg, #181818 0%, #242424 100%);
                border: 1px solid rgba(255, 255, 255, 0.12);
                border-radius: 16px;
                padding: 36px;
                max-width: 550px;
                width: 90%;
                max-height: 85vh;
                overflow-y: auto;
                box-shadow: 0 24px 70px rgba(0, 0, 0, 0.7);
            ">
                <div style="text-align: center; margin-bottom: 24px;">
                    <div style="font-size: 52px; margin-bottom: 8px; filter: drop-shadow(0 4px 12px rgba(229,9,20,0.4));">🎉</div>
                    <h2 style="color: #fff; font-size: 26px; font-weight: 700; margin: 0 0 6px 0; letter-spacing: -0.5px;">Update Available!</h2>
                    <p style="color: #e50914; font-size: 16px; font-weight: 600; margin: 0;">
                        Version ${updateInfo.version}${sizeText}
                    </p>
                    <p style="color: #888; font-size: 13px; margin: 4px 0 0 0;">
                        Current version: ${updateInfo.currentVersion}
                    </p>
                </div>

                <div id="ota-update-info">
                    <div style="
                        background: rgba(255, 255, 255, 0.05);
                        border: 1px solid rgba(255, 255, 255, 0.08);
                        border-radius: 10px;
                        padding: 18px;
                        margin-bottom: 24px;
                        max-height: 220px;
                        overflow-y: auto;
                    ">
                        <h3 style="color: #fff; font-size: 15px; font-weight: 600; margin: 0 0 10px 0;">What's New:</h3>
                        <div style="color: #ccc; font-size: 14px; line-height: 1.6;">
                            ${releaseNotesHtml}
                        </div>
                    </div>
                </div>

                <div id="ota-progress-container" style="display: none; text-align: center; padding: 12px 0 24px 0;">
                    <div style="font-size: 1rem; color: #fff; font-weight: 600; margin-bottom: 10px;" id="ota-progress-status">Downloading update from GitHub...</div>
                    <div style="width: 100%; height: 10px; background: rgba(255, 255, 255, 0.1); border-radius: 999px; overflow: hidden; margin-bottom: 10px;">
                        <div id="ota-progress-bar" style="width: 0%; height: 100%; background: linear-gradient(90deg, #e50914, #ff5252); transition: width 0.3s ease;"></div>
                    </div>
                    <div style="font-size: 0.85rem; color: #aaa;" id="ota-progress-subtext">Please wait... Lanflix will restart automatically.</div>
                </div>

                <div style="display: flex; gap: 10px; justify-content: center; flex-wrap: wrap;" id="ota-modal-buttons">
                    <button id="update-now-btn" style="
                        background: #e50914;
                        color: white;
                        border: none;
                        padding: 14px 28px;
                        font-size: 15px;
                        font-weight: 600;
                        border-radius: 8px;
                        cursor: pointer;
                        transition: all 0.2s;
                        flex: 1;
                        min-width: 130px;
                    " onmouseover="this.style.background='#f40612'; this.style.transform='scale(1.03)'" 
                       onmouseout="this.style.background='#e50914'; this.style.transform='scale(1)'">
                        Update Now
                    </button>
                    <button id="update-later-btn" style="
                        background: rgba(255, 255, 255, 0.1);
                        color: white;
                        border: 1px solid rgba(255, 255, 255, 0.15);
                        padding: 14px 24px;
                        font-size: 15px;
                        font-weight: 600;
                        border-radius: 8px;
                        cursor: pointer;
                        transition: all 0.2s;
                        flex: 1;
                        min-width: 110px;
                    " onmouseover="this.style.background='rgba(255, 255, 255, 0.18)'" 
                       onmouseout="this.style.background='rgba(255, 255, 255, 0.1)'">
                        Later
                    </button>
                    <button id="update-skip-btn" style="
                        background: transparent;
                        color: #888;
                        border: none;
                        padding: 14px 16px;
                        font-size: 13px;
                        cursor: pointer;
                        transition: color 0.2s;
                    " onmouseover="this.style.color='#fff'" 
                       onmouseout="this.style.color='#888'">
                        Skip Version
                    </button>
                </div>
            </div>
        `;

        document.body.appendChild(modal);

        document.getElementById('update-now-btn').onclick = () => {
            this.applyUpdate(updateInfo.downloadUrl);
        };

        document.getElementById('update-later-btn').onclick = () => {
            this.hideUpdateNotification();
        };

        document.getElementById('update-skip-btn').onclick = () => {
            localStorage.setItem(this.skipVersionKey, updateInfo.version);
            this.hideUpdateNotification();
        };
    }

    showUpdateModal(updateInfo) {
        this.showUpdateNotification(updateInfo);
    }

    hideUpdateNotification() {
        if (this.isUpdating) return;
        const modal = document.getElementById('update-notification-modal');
        if (modal) modal.remove();
        const otaModal = document.getElementById('ota-update-modal');
        if (otaModal) otaModal.remove();
    }

    hideUpdateModal() {
        this.hideUpdateNotification();
    }

    formatReleaseNotes(notes) {
        if (!notes) return '<p>Bug fixes and performance improvements.</p>';
        let formatted = notes
            .replace(/^### (.+)$/gm, '<h4 style="color: #fff; margin: 12px 0 6px 0;">$1</h4>')
            .replace(/^## (.+)$/gm, '<h3 style="color: #fff; margin: 16px 0 8px 0;">$1</h3>')
            .replace(/^- (.+)$/gm, '<li style="margin: 4px 0;">$1</li>')
            .replace(/\*\*(.+?)\*\*/g, '<strong>$1</strong>')
            .replace(/\n\n/g, '</p><p style="margin: 8px 0;">');

        if (formatted.includes('<li')) {
            formatted = formatted.replace(/(<li[^>]*>.*<\/li>)/s, '<ul style="margin: 8px 0; padding-left: 20px;">$1</ul>');
        }
        return formatted;
    }

    async applyUpdate(downloadUrl) {
        this.isUpdating = true;
        const infoContainer = document.getElementById('ota-update-info');
        const progressContainer = document.getElementById('ota-progress-container');
        const modalButtons = document.getElementById('ota-modal-buttons');
        const progressBar = document.getElementById('ota-progress-bar');
        const progressStatus = document.getElementById('ota-progress-status');

        if (infoContainer) infoContainer.style.display = 'none';
        if (modalButtons) modalButtons.style.display = 'none';
        if (progressContainer) progressContainer.style.display = 'block';

        if (progressBar) progressBar.style.width = '10%';
        if (progressStatus) progressStatus.textContent = 'Connecting to GitHub...';

        this.startProgressPolling();

        try {
            const response = await fetch('/api/server-update/apply', {
                method: 'POST',
                headers: { 'Content-Type': 'application/json' },
                body: JSON.stringify({ downloadUrl: downloadUrl })
            });

            if (!response.ok) {
                const errorData = await response.json();
                throw new Error(errorData.error || 'Server rejected update request');
            }
        } catch (error) {
            console.error('Error starting server update:', error);
            this.stopProgressPolling();
            this.isUpdating = false;
            if (progressStatus) progressStatus.textContent = 'Update Failed!';
            if (modalButtons) modalButtons.style.display = 'flex';
        }
    }

    startProgressPolling() {
        this.stopProgressPolling();
        let errorCount = 0;
        let hasSeenActiveProgress = false;

        this.progressInterval = setInterval(async () => {
            try {
                const response = await fetch('/api/server-update/progress', { cache: 'no-store' });
                if (response.ok) {
                    errorCount = 0;
                    const progress = await response.json();

                    if (progress.status === 'Downloading' || progress.status === 'Extracting' || progress.status === 'Applying') {
                        hasSeenActiveProgress = true;
                        this.updateProgressUI(progress);
                    } else if (progress.status === 'Complete' || (hasSeenActiveProgress && progress.status === 'Idle')) {
                        this.stopProgressPolling();
                        this.handleUpdateComplete();
                    } else if (progress.status) {
                        this.updateProgressUI(progress);
                    }
                }
            } catch (e) {
                errorCount++;
                if (errorCount > 3) {
                    const progressStatus = document.getElementById('ota-progress-status');
                    const progressSubtext = document.getElementById('ota-progress-subtext');
                    const progressBar = document.getElementById('ota-progress-bar');
                    if (progressBar) progressBar.style.width = '95%';
                    if (progressStatus) progressStatus.textContent = 'Restarting Lanflix Server...';
                    if (progressSubtext) progressSubtext.textContent = 'Applying updates and launching new version...';
                    
                    this.pollForServerReboot();
                }
            }
        }, 800);
    }

    handleUpdateComplete() {
        this.stopProgressPolling();
        const progressBar = document.getElementById('ota-progress-bar');
        const progressStatus = document.getElementById('ota-progress-status');
        const progressSubtext = document.getElementById('ota-progress-subtext');

        if (progressBar) progressBar.style.width = '100%';
        if (progressStatus) progressStatus.textContent = '🎉 Update Complete!';
        if (progressSubtext) progressSubtext.textContent = 'Lanflix restarted successfully! Reloading...';

        this.triggerConfetti();

        setTimeout(() => {
            window.location.reload();
        }, 1200);
    }

    updateProgressUI(progress) {
        const progressBar = document.getElementById('ota-progress-bar');
        const progressStatus = document.getElementById('ota-progress-status');
        const progressSubtext = document.getElementById('ota-progress-subtext');

        if (progressBar && progress.percentage) {
            progressBar.style.width = `${Math.min(100, Math.max(10, progress.percentage))}%`;
        }

        if (progressStatus && progress.status) {
            progressStatus.textContent = `${progress.status}... (${progress.percentage}%)`;
        }

        if (progressSubtext && progress.message) {
            progressSubtext.textContent = progress.message;
        }
    }

    pollForServerReboot() {
        this.stopProgressPolling();
        const interval = setInterval(async () => {
            try {
                const response = await fetch('/api/server-update/version', { cache: 'no-store' });
                if (response.ok) {
                    clearInterval(interval);
                    this.handleUpdateComplete();
                }
            } catch (e) {
                // Server rebooting
            }
        }, 1500);
    }

    stopProgressPolling() {
        if (this.progressInterval) {
            clearInterval(this.progressInterval);
            this.progressInterval = null;
        }
    }

    showNoUpdateMessage(msg = '✅ You are running the latest version of Lanflix!') {
        this.showToast(msg);
    }

    showErrorMessage(msg) {
        this.showToast(msg, true);
    }

    showToast(message, isError = false) {
        const toast = document.createElement('div');
        toast.style.cssText = `
            position: fixed;
            top: 80px;
            left: 50%;
            transform: translateX(-50%);
            background: ${isError ? 'rgba(229, 9, 20, 0.95)' : 'rgba(255, 255, 255, 0.95)'};
            color: ${isError ? '#fff' : '#000'};
            padding: 14px 28px;
            border-radius: 8px;
            font-size: 15px;
            font-weight: 600;
            z-index: 10002;
            box-shadow: 0 4px 20px rgba(0, 0, 0, 0.4);
            animation: fadeIn 0.2s ease-out;
        `;
        toast.textContent = message;
        document.body.appendChild(toast);
        setTimeout(() => toast.remove(), 3500);
    }
}

export const appUpdater = new AppUpdater();
if (typeof window !== 'undefined') {
    window.appUpdater = appUpdater;
}

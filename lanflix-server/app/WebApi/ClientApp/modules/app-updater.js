/**
 * App & Server Unified OTA Updater Module
 * Handles Cozmo-style OTA downloads, progress tracking, and live web reboots
 */

export class AppUpdater {
    constructor() {
        this.currentVersion = '1.2.6';
        this.progressInterval = null;
        this.isUpdating = false;
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
                    </svg> Checking GitHub...`;
            }

            const response = await fetch('/api/server-update/check', { cache: 'no-store' });
            const data = await response.json();

            if (checkBtn && userInitiated) {
                checkBtn.disabled = false;
                checkBtn.innerHTML = originalHtml;
            }

            if (data.updateAvailable) {
                this.showUpdateModal({
                    version: data.latestVersion,
                    currentVersion: data.currentVersion || this.currentVersion,
                    releaseNotes: data.releaseNotes,
                    downloadUrl: data.downloadUrl,
                    fileSize: data.fileSize
                });
                return true;
            } else {
                if (userInitiated) {
                    this.showToast(`Lanflix is up to date! (Version ${data.currentVersion || this.currentVersion})`);
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
                this.showToast('Could not check for updates. Check server internet connection.', true);
            }
            return false;
        }
    }

    showUpdateModal(updateInfo) {
        this.hideUpdateModal();

        let modal = document.getElementById('ota-update-modal');
        if (!modal) {
            modal = document.createElement('div');
            modal.id = 'ota-update-modal';
            modal.className = 'modal-overlay active';
            modal.innerHTML = `
                <div class="modal" style="max-width: 500px;">
                    <div class="modal-header">
                        <h2 class="modal-title" id="ota-modal-title">Lanflix Update Available</h2>
                        <button class="modal-close" id="close-ota-modal" aria-label="Close">
                            <svg viewBox="0 0 24 24">
                                <path d="M19 6.41L17.59 5 12 10.59 6.41 5 5 6.41 10.59 12 5 17.59 6.41 19 12 13.41 17.59 19 19 17.59 13.41 12z" />
                            </svg>
                        </button>
                    </div>
                    <div class="modal-body">
                        <div id="ota-update-info">
                            <div style="display: flex; align-items: center; justify-content: space-between; margin-bottom: 12px;">
                                <span style="font-weight: 600; color: #fff; font-size: 1.1rem;" id="ota-version-text">Version ${updateInfo.version}</span>
                                <span style="background: rgba(229, 9, 20, 0.2); color: #e50914; padding: 4px 10px; border-radius: 999px; font-weight: 700; font-size: 0.8rem;" id="ota-size-badge">New Release</span>
                            </div>
                            <div style="background: rgba(255, 255, 255, 0.05); padding: 14px; border-radius: 10px; max-height: 160px; overflow-y: auto; margin-bottom: 16px;">
                                <p style="font-size: 0.85rem; color: #bbb; margin-bottom: 6px; font-weight: 600;">What's New:</p>
                                <div id="ota-release-notes" style="font-size: 0.85rem; color: #ddd; white-space: pre-line;">${updateInfo.releaseNotes || 'Bug fixes and performance improvements.'}</div>
                            </div>
                        </div>
                        <div id="ota-progress-container" style="display: none; text-align: center; padding: 16px 0;">
                            <div style="font-size: 1rem; color: #fff; font-weight: 600; margin-bottom: 8px;" id="ota-progress-status">Downloading update from GitHub...</div>
                            <div style="width: 100%; height: 8px; background: rgba(255, 255, 255, 0.1); border-radius: 4px; overflow: hidden; margin-bottom: 8px;">
                                <div id="ota-progress-bar" style="width: 0%; height: 100%; background: #e50914; transition: width 0.3s ease;"></div>
                            </div>
                            <div style="font-size: 0.8rem; color: #aaa;" id="ota-progress-subtext">Please wait... Lanflix will restart automatically.</div>
                        </div>
                    </div>
                    <div class="modal-footer" id="ota-modal-footer">
                        <button class="modal-btn modal-btn-secondary" id="cancel-ota-btn">Later</button>
                        <button class="modal-btn modal-btn-primary" id="apply-ota-btn" style="background: #e50914; border-color: #e50914;">Download &amp; Update Now</button>
                    </div>
                </div>
            `;
            document.body.appendChild(modal);
        } else {
            modal.classList.add('active');
            const versionText = document.getElementById('ota-version-text');
            const releaseNotes = document.getElementById('ota-release-notes');
            if (versionText) versionText.textContent = `Version ${updateInfo.version}`;
            if (releaseNotes) releaseNotes.textContent = updateInfo.releaseNotes || 'Bug fixes and performance improvements.';
        }

        const closeBtn = document.getElementById('close-ota-modal');
        const cancelBtn = document.getElementById('cancel-ota-btn');
        const applyBtn = document.getElementById('apply-ota-btn');

        if (closeBtn) closeBtn.onclick = () => this.hideUpdateModal();
        if (cancelBtn) cancelBtn.onclick = () => this.hideUpdateModal();
        if (applyBtn) {
            applyBtn.onclick = () => {
                this.applyUpdate(updateInfo.downloadUrl);
            };
        }
    }

    hideUpdateModal() {
        if (this.isUpdating) return;
        const modal = document.getElementById('ota-update-modal');
        if (modal) {
            modal.classList.remove('active');
        }
    }

    async applyUpdate(downloadUrl) {
        this.isUpdating = true;
        const infoContainer = document.getElementById('ota-update-info');
        const progressContainer = document.getElementById('ota-progress-container');
        const modalFooter = document.getElementById('ota-modal-footer');
        const progressBar = document.getElementById('ota-progress-bar');
        const progressStatus = document.getElementById('ota-progress-status');
        const progressSubtext = document.getElementById('ota-progress-subtext');

        if (infoContainer) infoContainer.style.display = 'none';
        if (modalFooter) modalFooter.style.display = 'none';
        if (progressContainer) progressContainer.style.display = 'block';

        if (progressBar) progressBar.style.width = '10%';
        if (progressStatus) progressStatus.textContent = 'Connecting to GitHub...';

        // Start live progress polling (Cozmo-style OTA reporting)
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
            if (progressSubtext) progressSubtext.textContent = error.message;
            if (modalFooter) modalFooter.style.display = 'flex';
        }
    }

    startProgressPolling() {
        this.stopProgressPolling();
        let errorCount = 0;

        this.progressInterval = setInterval(async () => {
            try {
                const response = await fetch('/api/server-update/progress', { cache: 'no-store' });
                if (response.ok) {
                    errorCount = 0;
                    const progress = await response.json();
                    this.updateProgressUI(progress);

                    if (progress.status === 'Complete') {
                        this.stopProgressPolling();
                        this.handleUpdateComplete();
                    }
                }
            } catch (e) {
                errorCount++;
                if (errorCount > 3) {
                    // Server is rebooting!
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
                    const progressBar = document.getElementById('ota-progress-bar');
                    const progressStatus = document.getElementById('ota-progress-status');
                    if (progressBar) progressBar.style.width = '100%';
                    if (progressStatus) progressStatus.textContent = 'Update Applied! Reloading...';

                    setTimeout(() => {
                        window.location.reload();
                    }, 1000);
                }
            } catch (e) {
                // Server is still starting up
            }
        }, 1500);
    }

    stopProgressPolling() {
        if (this.progressInterval) {
            clearInterval(this.progressInterval);
            this.progressInterval = null;
        }
    }

    /**
     * Unified entry point called by settings-main.js for both server and Android updates.
     * isServerUpdate=true  → triggers the OTA progress modal + /api/server-update/apply
     * isServerUpdate=false → tells the Android WebAppInterface to start APK install
     */
    showUpdateNotification(updateInfo) {
        if (updateInfo.isServerUpdate) {
            this.showUpdateModal(updateInfo);
        } else {
            // Android app update – delegate to native bridge or show install modal
            this._showAppUpdateModal(updateInfo);
        }
    }

    showNoUpdateMessage() {
        this.showToast(`✅ Lanflix is up to date! (v${this.currentVersion})`);
    }

    showErrorMessage(msg) {
        this.showToast(msg, true);
    }

    _showAppUpdateModal(updateInfo) {
        this.hideUpdateModal();

        let modal = document.getElementById('ota-update-modal');
        if (!modal) {
            modal = document.createElement('div');
            modal.id = 'ota-update-modal';
            modal.className = 'modal-overlay active';
            document.body.appendChild(modal);
        } else {
            modal.classList.add('active');
        }

        modal.innerHTML = `
            <div class="modal" style="max-width: 500px;">
                <div class="modal-header">
                    <h2 class="modal-title">App Update Available</h2>
                    <button class="modal-close" id="close-ota-modal" aria-label="Close">
                        <svg viewBox="0 0 24 24"><path d="M19 6.41L17.59 5 12 10.59 6.41 5 5 6.41 10.59 12 5 17.59 6.41 19 12 13.41 17.59 19 19 17.59 13.41 12z" /></svg>
                    </button>
                </div>
                <div class="modal-body">
                    <div style="display: flex; align-items: center; justify-content: space-between; margin-bottom: 12px;">
                        <span style="font-weight: 600; color: #fff; font-size: 1.1rem;">Version ${updateInfo.version}</span>
                        <span style="background: rgba(229, 9, 20, 0.2); color: #e50914; padding: 4px 10px; border-radius: 999px; font-weight: 700; font-size: 0.8rem;">New Release</span>
                    </div>
                    <div style="background: rgba(255,255,255,0.05); padding: 14px; border-radius: 10px; max-height: 160px; overflow-y: auto; margin-bottom: 16px;">
                        <p style="font-size: 0.85rem; color: #bbb; margin-bottom: 6px; font-weight: 600;">What's New:</p>
                        <div style="font-size: 0.85rem; color: #ddd; white-space: pre-line;">${updateInfo.releaseNotes || 'Bug fixes and improvements.'}</div>
                    </div>
                </div>
                <div class="modal-footer">
                    <button class="modal-btn modal-btn-secondary" id="cancel-ota-btn">Later</button>
                    <button class="modal-btn modal-btn-primary" id="apply-ota-btn" style="background: #e50914; border-color: #e50914;">Install Update</button>
                </div>
            </div>
        `;

        document.getElementById('close-ota-modal').onclick = () => this.hideUpdateModal();
        document.getElementById('cancel-ota-btn').onclick = () => this.hideUpdateModal();
        document.getElementById('apply-ota-btn').onclick = () => {
            if (window.Android && window.Android.downloadApk) {
                window.Android.downloadApk(updateInfo.downloadUrl);
            } else {
                window.open(updateInfo.downloadUrl, '_blank');
            }
            this.hideUpdateModal();
        };
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

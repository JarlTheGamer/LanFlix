import apiClient from './api-client.js';

export class DevicePairingManager {
  constructor() {
    this.pollInterval = null;
  }

  async checkAndEnforcePairing() {
    try {
      const device = await apiClient.registerDevice();
      if (!device || device.isPaired) {
        this.hidePairingOverlay();
        return true;
      }

      // Show pairing code screen and start polling
      this.showPairingOverlay(device.pairingCode);
      this.startPollingStatus();
      return false;
    } catch (e) {
      console.warn('Device pairing check failed:', e);
      return true; // Don't block if API fails
    }
  }

  showPairingOverlay(pairingCode) {
    let overlay = document.getElementById('device-pairing-overlay');
    if (!overlay) {
      overlay = document.createElement('div');
      overlay.id = 'device-pairing-overlay';
      overlay.style.cssText = `
        position: fixed;
        top: 0; left: 0; width: 100vw; height: 100vh;
        background: rgba(10, 12, 18, 0.96);
        backdrop-filter: blur(25px);
        z-index: 999999;
        display: flex;
        align-items: center;
        justify-content: center;
        color: #fff;
        font-family: 'Poppins', system-ui, -apple-system, sans-serif;
      `;
      document.body.appendChild(overlay);
    }

    overlay.innerHTML = `
      <div style="text-align: center; max-width: 500px; padding: 40px; background: rgba(255,255,255,0.03); border: 1px solid rgba(255,255,255,0.1); border-radius: 24px; box-shadow: 0 20px 50px rgba(0,0,0,0.5);">
        <div style="width: 70px; height: 70px; margin: 0 auto 20px; background: linear-gradient(135deg, #e50914, #b20710); border-radius: 20px; display: flex; align-items: center; justify-content: center; box-shadow: 0 10px 25px rgba(229,9,20,0.4);">
          <svg width="36" height="36" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2"><rect x="2" y="3" width="20" height="14" rx="2" ry="2"></rect><line x1="8" y1="21" x2="16" y2="21"></line><line x1="12" y1="17" x2="12" y2="21"></line></svg>
        </div>
        <h1 style="font-size: 28px; font-weight: 700; margin-bottom: 12px; letter-spacing: -0.5px;">Device Pairing Required</h1>
        <p style="color: #a0a5b5; font-size: 15px; line-height: 1.6; margin-bottom: 30px;">To connect this screen to your Lanflix Server, enter this 6-character code in <strong>Settings → Devices</strong> on an authorized device.</p>
        
        <div style="background: rgba(0,0,0,0.4); border: 2px dashed rgba(229,9,20,0.6); padding: 20px; border-radius: 16px; margin-bottom: 24px;">
          <span style="font-family: monospace; font-size: 42px; font-weight: 800; letter-spacing: 8px; color: #e50914; text-shadow: 0 0 20px rgba(229,9,20,0.5);">${pairingCode}</span>
        </div>

        <div style="display: flex; align-items: center; justify-content: center; gap: 10px; color: #707585; font-size: 13px;">
          <div style="width: 8px; height: 8px; background: #e50914; border-radius: 50%; animation: pulse 1.5s infinite;"></div>
          Waiting for server approval...
        </div>
      </div>
      <style>
        @keyframes pulse { 0%, 100% { opacity: 0.2; transform: scale(0.8); } 50% { opacity: 1; transform: scale(1.2); } }
      </style>
    `;
  }

  hidePairingOverlay() {
    if (this.pollInterval) {
      clearInterval(this.pollInterval);
      this.pollInterval = null;
    }
    const overlay = document.getElementById('device-pairing-overlay');
    if (overlay) {
      overlay.remove();
    }
  }

  startPollingStatus() {
    if (this.pollInterval) return;
    this.pollInterval = setInterval(async () => {
      try {
        const device = await apiClient.checkDeviceStatus();
        if (device && device.isPaired) {
          this.hidePairingOverlay();
          window.location.reload();
        }
      } catch (e) {}
    }, 2000);
  }
}

export const devicePairingManager = new DevicePairingManager();

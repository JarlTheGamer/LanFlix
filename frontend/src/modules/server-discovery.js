/**
 * Server Discovery Module
 * Automatically discovers Lanflix server on local network
 */

export class ServerDiscovery {
  constructor() {
    this.ports = [6129]; // Primary port only for fast discovery
    this.timeout = 1000; // 1 second timeout per attempt
    this.healthEndpoint = '/health';
  }

  /**
   * Try to connect to a specific IP address on multiple ports
   */
  async tryServer(ip) {
    // Try all ports in parallel for this IP
    const portPromises = this.ports.map(port => this.tryServerPort(ip, port));
    const results = await Promise.all(portPromises);

    // Return the first successful connection
    return results.find(result => result !== null) || null;
  }

  /**
   * Try to connect to a specific IP address and port
   */
  async tryServerPort(ip, port) {
    const url = `http://${ip}:${port}${this.healthEndpoint}`;

    try {
      const controller = new AbortController();
      const timeoutId = setTimeout(() => controller.abort(), this.timeout);

      const response = await fetch(url, {
        method: 'GET',
        signal: controller.signal,
        headers: { 'Accept': 'application/json' }
      });

      clearTimeout(timeoutId);

      if (response.ok) {
        const data = await response.json();
        if (data.status === 'ok') {
          console.log(`✅ Found server at ${ip}:${port}`);
          return `http://${ip}:${port}`;
        }
      }
    } catch (error) {
      // Connection failed, server not found at this IP:port
      return null;
    }

    return null;
  }

  /**
   * Get local IP address range to scan
   * Returns common private network ranges with smart ordering
   */
  getLocalNetworkRanges() {
    // Most common home network ranges, ordered by likelihood
    return [
      // 192.168.1.x (most common)
      { base: '192.168.1', start: 1, end: 254 },
      // 192.168.0.x (second most common)
      { base: '192.168.0', start: 1, end: 254 },
      // Other common ranges
      { base: '192.168.2', start: 1, end: 254 },
      { base: '10.0.0', start: 1, end: 254 },
      { base: '10.0.1', start: 1, end: 254 },
    ];
  }

  /**
   * Generate IP addresses to scan based on common patterns
   * Smart ordering: most likely IPs first
   */
  generateScanList() {
    const ips = [];

    // Tier 1: Most common server IPs (router and common static IPs)
    const tier1 = [
      '192.168.1.1',
      '192.168.0.1',
      '192.168.1.100',
      '192.168.0.100',
      '10.0.0.1',
      '192.168.1.10',
      '192.168.0.10',
    ];

    // Tier 2: Common DHCP range (2-50)
    const tier2 = [];
    for (let i = 2; i <= 50; i++) {
      tier2.push(`192.168.1.${i}`);
      tier2.push(`192.168.0.${i}`);
    }

    // Tier 3: Extended range (51-254)
    const tier3 = [];
    for (let i = 51; i <= 254; i++) {
      tier3.push(`192.168.1.${i}`);
      tier3.push(`192.168.0.${i}`);
    }

    // Tier 4: Other networks
    const tier4 = [];
    const otherRanges = [
      { base: '192.168.2', start: 1, end: 254 },
      { base: '10.0.0', start: 2, end: 254 },
      { base: '10.0.1', start: 1, end: 254 },
    ];

    for (const range of otherRanges) {
      for (let i = range.start; i <= range.end; i++) {
        tier4.push(`${range.base}.${i}`);
      }
    }

    // Combine all tiers
    return [...tier1, ...tier2, ...tier3, ...tier4];
  }

  /**
   * Discover server on local network
   * Uses parallel scanning with smart batching for speed
   */
  async discover(onProgress = null) {
    const ips = this.generateScanList();
    const batchSize = 50; // Scan 50 IPs at a time for speed
    let scanned = 0;

    console.log(`🔍 Starting smart server discovery...`);

    // Scan in batches, stop as soon as we find one
    for (let i = 0; i < ips.length; i += batchSize) {
      const batch = ips.slice(i, i + batchSize);

      // Try all IPs in batch simultaneously
      const results = await Promise.all(
        batch.map(ip => this.tryServer(ip))
      );

      // Check if we found a server
      const found = results.find(result => result !== null);
      if (found) {
        console.log(`✅ Server found at: ${found}`);
        return found;
      }

      // Update progress
      scanned += batch.length;
      if (onProgress) {
        const percent = Math.round((scanned / ips.length) * 100);
        onProgress(scanned, ips.length);

        // Early exit if we've scanned enough without finding anything
        if (scanned >= 100 && percent >= 10) {
          console.log(`⚠️ Scanned ${scanned} addresses without finding server. Stopping early.`);
          break;
        }
      }
    }

    console.log('❌ No server found on local network');
    return null;
  }

  /**
   * Quick discovery - only checks most likely IPs
   * Useful for fast reconnection attempts
   */
  async quickDiscover() {
    const priorityIPs = [
      '192.168.1.1',
      '192.168.0.1',
      '192.168.1.100',
      '192.168.0.100',
      '192.168.1.10',
      '192.168.0.10',
      '10.0.0.1',
      '192.168.1.2',
      '192.168.0.2',
    ];

    console.log(`🔍 Quick server discovery (port 6129)...`);

    // Try all priority IPs in parallel
    const results = await Promise.all(
      priorityIPs.map(ip => this.tryServer(ip))
    );

    const found = results.find(result => result !== null);
    if (found) {
      console.log(`✅ Server found at: ${found}`);
      return found;
    }

    console.log('⚠️ Server not found in quick scan, trying full scan...');
    return null;
  }

  /**
   * Verify a specific server URL
   */
  async verify(url) {
    try {
      // Remove trailing slash and /api if present
      const cleanUrl = url.replace(/\/$/, '').replace(/\/api$/, '');
      const healthUrl = `${cleanUrl}${this.healthEndpoint}`;

      const response = await fetch(healthUrl, {
        method: 'GET',
        headers: { 'Accept': 'application/json' }
      });

      if (response.ok) {
        const data = await response.json();
        return data.status === 'ok';
      }
    } catch (error) {
      console.error('Server verification failed:', error);
    }

    return false;
  }
}

export const serverDiscovery = new ServerDiscovery();

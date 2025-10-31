/**
 * Server Discovery Module
 * Automatically discovers Lanflix server on local network
 */

export class ServerDiscovery {
  constructor() {
    this.port = 6129; // Custom Lanflix port
    this.timeout = 2000; // 2 second timeout per attempt
    this.healthEndpoint = '/health';
  }

  /**
   * Try to connect to a specific IP address
   */
  async tryServer(ip) {
    const url = `http://${ip}:${this.port}${this.healthEndpoint}`;
    
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
          return `http://${ip}:${this.port}`;
        }
      }
    } catch (error) {
      // Connection failed, server not found at this IP
      return null;
    }
    
    return null;
  }

  /**
   * Get local IP address range to scan
   * Returns common private network ranges
   */
  getLocalNetworkRanges() {
    // Common private network ranges
    return [
      // 192.168.x.x (most common home networks)
      { base: '192.168.1', start: 1, end: 254 },
      { base: '192.168.0', start: 1, end: 254 },
      { base: '192.168.2', start: 1, end: 254 },
      // 10.x.x.x (some routers)
      { base: '10.0.0', start: 1, end: 254 },
      { base: '10.0.1', start: 1, end: 254 },
    ];
  }

  /**
   * Generate IP addresses to scan based on common patterns
   */
  generateScanList() {
    const ips = [];
    
    // Priority IPs (most common server addresses)
    const priorityIPs = [
      'localhost',
      '127.0.0.1',
      '192.168.1.1',
      '192.168.1.100',
      '192.168.0.1',
      '192.168.0.100',
      '10.0.0.1',
      '10.0.0.100',
    ];
    
    ips.push(...priorityIPs);
    
    // Add ranges for thorough scan
    const ranges = this.getLocalNetworkRanges();
    for (const range of ranges) {
      for (let i = range.start; i <= range.end; i++) {
        const ip = `${range.base}.${i}`;
        if (!priorityIPs.includes(ip)) {
          ips.push(ip);
        }
      }
    }
    
    return ips;
  }

  /**
   * Discover server on local network
   * Uses parallel scanning with batching for speed
   */
  async discover(onProgress = null) {
    const ips = this.generateScanList();
    const batchSize = 20; // Scan 20 IPs at a time
    let scanned = 0;
    
    console.log(`🔍 Starting server discovery (scanning ${ips.length} addresses)...`);
    
    // Scan in batches
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
        onProgress(scanned, ips.length);
      }
    }
    
    console.log('❌ No server found on local network');
    return null;
  }

  /**
   * Quick discovery - only checks priority IPs
   * Useful for fast reconnection attempts
   */
  async quickDiscover() {
    const priorityIPs = [
      'localhost',
      '127.0.0.1',
      '192.168.1.1',
      '192.168.1.100',
      '192.168.0.1',
      '192.168.0.100',
      '10.0.0.1',
      '10.0.0.100',
    ];
    
    console.log('🔍 Quick server discovery...');
    
    const results = await Promise.all(
      priorityIPs.map(ip => this.tryServer(ip))
    );
    
    const found = results.find(result => result !== null);
    if (found) {
      console.log(`✅ Server found at: ${found}`);
      return found;
    }
    
    console.log('❌ Server not found in quick scan');
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

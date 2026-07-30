using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Text;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Lanflix.Infrastructure.Services.Discovery;

public class MDnsDiscoveryService : BackgroundService
{
    private const string MulticastAddress = "224.0.0.251";
    private const int MDnsPort = 5353;
    private const string HostName = "lanflix.local";
    private const string ServiceType = "_lanflix._tcp.local";
    private const string InstanceName = "Lanflix._lanflix._tcp.local";
    private const int DefaultServerPort = 5037;

    private readonly ILogger<MDnsDiscoveryService> _logger;

    public MDnsDiscoveryService(ILogger<MDnsDiscoveryService> logger)
    {
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Starting Lanflix mDNS Responder service (Host: {HostName}, Service: {ServiceType})...", HostName, ServiceType);

        var physicalIps = GetPhysicalIPv4Addresses();
        _logger.LogInformation("Physical LAN IP addresses identified for mDNS: {IPs}", string.Join(", ", physicalIps));

        UdpClient? udpClient = null;
        try
        {
            udpClient = new UdpClient();
            udpClient.Client.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, true);
            udpClient.Client.Bind(new IPEndPoint(IPAddress.Any, MDnsPort));

            var multicastIp = IPAddress.Parse(MulticastAddress);

            foreach (var ip in physicalIps)
            {
                try
                {
                    udpClient.JoinMulticastGroup(multicastIp, ip);
                    _logger.LogInformation("Joined mDNS multicast group 224.0.0.251 on interface {IP}", ip);
                }
                catch (Exception ex)
                {
                    _logger.LogDebug(ex, "Could not join multicast group on interface {IP}", ip);
                }
            }

            try
            {
                udpClient.JoinMulticastGroup(multicastIp);
            }
            catch { }

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    var result = await udpClient.ReceiveAsync(stoppingToken);
                    ProcessMDnsPacket(udpClient, result.Buffer, result.RemoteEndPoint, physicalIps);
                }
                catch (OperationCanceledException)
                {
                    break;
                }
                catch (Exception ex)
                {
                    _logger.LogDebug(ex, "Error processing incoming mDNS packet");
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "mDNS Responder service encountered an error on startup");
        }
        finally
        {
            udpClient?.Dispose();
            _logger.LogInformation("Lanflix mDNS Responder service stopped.");
        }
    }

    private void ProcessMDnsPacket(UdpClient udpClient, byte[] packet, IPEndPoint remoteEndPoint, List<IPAddress> physicalIps)
    {
        if (packet.Length < 12) return;

        ushort flags = (ushort)((packet[2] << 8) | packet[3]);
        bool isQuery = (flags & 0x8000) == 0;
        if (!isQuery) return;

        ushort qdCount = (ushort)((packet[4] << 8) | packet[5]);
        if (qdCount == 0) return;

        int offset = 12;
        for (int i = 0; i < qdCount; i++)
        {
            var name = ReadDnsName(packet, ref offset);
            if (offset + 4 > packet.Length) break;

            ushort type = (ushort)((packet[offset] << 8) | packet[offset + 1]);
            ushort qClass = (ushort)((packet[offset + 2] << 8) | packet[offset + 3]);
            offset += 4;

            if (string.Equals(name, HostName, StringComparison.OrdinalIgnoreCase) ||
                string.Equals(name, ServiceType, StringComparison.OrdinalIgnoreCase) ||
                string.Equals(name, InstanceName, StringComparison.OrdinalIgnoreCase) ||
                string.Equals(name, "lanflix", StringComparison.OrdinalIgnoreCase))
            {
                _logger.LogInformation("Received mDNS query for {QueryName} from {ClientIP}", name, remoteEndPoint.Address);
                SendMDnsResponse(udpClient, remoteEndPoint, physicalIps);
            }
        }
    }

    private void SendMDnsResponse(UdpClient udpClient, IPEndPoint targetEndPoint, List<IPAddress> physicalIps)
    {
        try
        {
            var primaryIp = physicalIps.FirstOrDefault() ?? GetFallbackIPv4Address();
            if (primaryIp == null) return;

            var responseBytes = BuildFullDnsSdResponsePacket(primaryIp, DefaultServerPort);
            var multicastEndPoint = new IPEndPoint(IPAddress.Parse(MulticastAddress), MDnsPort);

            // 1. Send via multicast to default socket
            udpClient.Send(responseBytes, responseBytes.Length, multicastEndPoint);

            // 2. Send direct unicast to requesting client on its query port
            try
            {
                udpClient.Send(responseBytes, responseBytes.Length, targetEndPoint);
            }
            catch { }

            // 3. Send direct unicast to requesting client on standard mDNS port 5353
            try
            {
                var direct5353 = new IPEndPoint(targetEndPoint.Address, MDnsPort);
                udpClient.Send(responseBytes, responseBytes.Length, direct5353);
            }
            catch { }
        }
        catch (Exception ex)
        {
            _logger.LogDebug(ex, "Failed to send mDNS response packet");
        }
    }

    private static byte[] BuildFullDnsSdResponsePacket(IPAddress ipAddress, int port)
    {
        using var ms = new MemoryStream();
        using var bw = new BinaryWriter(ms);

        // Transaction ID (0 for mDNS)
        bw.Write((byte)0); bw.Write((byte)0);
        // Flags: Standard query response, Authoritative, No error (0x8400)
        bw.Write((byte)0x84); bw.Write((byte)0x00);
        // Questions count = 0
        bw.Write((byte)0); bw.Write((byte)0);
        // Answer RRs count = 4 (PTR, SRV, TXT, A)
        bw.Write((byte)0); bw.Write((byte)4);
        // Authority RRs = 0
        bw.Write((byte)0); bw.Write((byte)0);
        // Additional RRs = 0
        bw.Write((byte)0); bw.Write((byte)0);

        // Record 1: PTR Record (ServiceType -> InstanceName)
        WriteDnsName(bw, ServiceType);
        bw.Write((byte)0); bw.Write((byte)12); // Type PTR
        bw.Write((byte)0); bw.Write((byte)1);  // Class IN
        bw.Write((byte)0); bw.Write((byte)0); bw.Write((byte)0x00); bw.Write((byte)120); // TTL 120s
        var instanceBytes = EncodeDnsName(InstanceName);
        bw.Write((byte)0); bw.Write((byte)instanceBytes.Length);
        bw.Write(instanceBytes);

        // Record 2: SRV Record (InstanceName -> HostName:Port)
        WriteDnsName(bw, InstanceName);
        bw.Write((byte)0); bw.Write((byte)33); // Type SRV
        bw.Write((byte)0); bw.Write((byte)1);  // Class IN
        bw.Write((byte)0); bw.Write((byte)0); bw.Write((byte)0x00); bw.Write((byte)120); // TTL 120s
        var hostNameBytes = EncodeDnsName(HostName);
        ushort srvDataLen = (ushort)(6 + hostNameBytes.Length);
        bw.Write((byte)(srvDataLen >> 8)); bw.Write((byte)(srvDataLen & 0xFF));
        bw.Write((byte)0); bw.Write((byte)0); // Priority 0
        bw.Write((byte)0); bw.Write((byte)0); // Weight 0
        bw.Write((byte)(port >> 8)); bw.Write((byte)(port & 0xFF)); // Port 5037
        bw.Write(hostNameBytes);

        // Record 3: TXT Record (InstanceName -> metadata)
        WriteDnsName(bw, InstanceName);
        bw.Write((byte)0); bw.Write((byte)16); // Type TXT
        bw.Write((byte)0); bw.Write((byte)1);  // Class IN
        bw.Write((byte)0); bw.Write((byte)0); bw.Write((byte)0x00); bw.Write((byte)120); // TTL 120s
        byte[] txtBytes = Encoding.UTF8.GetBytes("txtvers=1");
        bw.Write((byte)0); bw.Write((byte)(txtBytes.Length + 1));
        bw.Write((byte)txtBytes.Length);
        bw.Write(txtBytes);

        // Record 4: A Record (HostName -> IP)
        WriteDnsName(bw, HostName);
        bw.Write((byte)0); bw.Write((byte)1);  // Type A
        bw.Write((byte)0); bw.Write((byte)1);  // Class IN
        bw.Write((byte)0); bw.Write((byte)0); bw.Write((byte)0x00); bw.Write((byte)120); // TTL 120s
        bw.Write((byte)0); bw.Write((byte)4);  // Data length 4
        bw.Write(ipAddress.GetAddressBytes());

        return ms.ToArray();
    }

    private static string ReadDnsName(byte[] packet, ref int offset)
    {
        var sb = new StringBuilder();
        while (offset < packet.Length)
        {
            byte len = packet[offset++];
            if (len == 0) break;
            if ((len & 0xC0) == 0xC0)
            {
                offset++;
                break;
            }
            if (offset + len > packet.Length) break;
            if (sb.Length > 0) sb.Append('.');
            sb.Append(Encoding.UTF8.GetString(packet, offset, len));
            offset += len;
        }
        return sb.ToString();
    }

    private static void WriteDnsName(BinaryWriter bw, string name)
    {
        bw.Write(EncodeDnsName(name));
    }

    private static byte[] EncodeDnsName(string name)
    {
        using var ms = new MemoryStream();
        var parts = name.Split('.');
        foreach (var part in parts)
        {
            var bytes = Encoding.UTF8.GetBytes(part);
            ms.WriteByte((byte)bytes.Length);
            ms.Write(bytes, 0, bytes.Length);
        }
        ms.WriteByte(0);
        return ms.ToArray();
    }

    private static List<IPAddress> GetPhysicalIPv4Addresses()
    {
        var list = new List<IPAddress>();
        try
        {
            foreach (var ni in NetworkInterface.GetAllNetworkInterfaces())
            {
                if (ni.OperationalStatus != OperationalStatus.Up) continue;
                if (ni.NetworkInterfaceType == NetworkInterfaceType.Loopback) continue;

                var name = ni.Name.ToLowerInvariant();
                var desc = ni.Description.ToLowerInvariant();

                if (name.Contains("vethernet") || name.Contains("wsl") || name.Contains("virtual") ||
                    name.Contains("hyper-v") || name.Contains("vmnet") || name.Contains("docker") || name.Contains("zerotier") ||
                    desc.Contains("virtual") || desc.Contains("hyper-v") || desc.Contains("vmware") || desc.Contains("wsl"))
                {
                    continue;
                }

                foreach (var ua in ni.GetIPProperties().UnicastAddresses)
                {
                    if (ua.Address.AddressFamily == AddressFamily.InterNetwork && !IPAddress.IsLoopback(ua.Address))
                    {
                        list.Add(ua.Address);
                    }
                }
            }
        }
        catch { }

        return list;
    }

    private static IPAddress? GetFallbackIPv4Address()
    {
        return NetworkInterface.GetAllNetworkInterfaces()
            .Where(ni => ni.OperationalStatus == OperationalStatus.Up && ni.NetworkInterfaceType != NetworkInterfaceType.Loopback)
            .SelectMany(ni => ni.GetIPProperties().UnicastAddresses)
            .FirstOrDefault(ua => ua.Address.AddressFamily == AddressFamily.InterNetwork && !IPAddress.IsLoopback(ua.Address))
            ?.Address;
    }
}

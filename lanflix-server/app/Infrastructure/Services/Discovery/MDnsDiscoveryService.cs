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
    private const string ServiceName = "_lanflix._tcp.local";
    private const int DefaultServerPort = 5037;

    private readonly ILogger<MDnsDiscoveryService> _logger;

    public MDnsDiscoveryService(ILogger<MDnsDiscoveryService> logger)
    {
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Starting Lanflix mDNS Responder service (Host: {HostName}, Service: {ServiceName})...", HostName, ServiceName);

        UdpClient? udpClient = null;
        try
        {
            udpClient = new UdpClient();
            udpClient.Client.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, true);
            udpClient.Client.Bind(new IPEndPoint(IPAddress.Any, MDnsPort));

            var multicastIp = IPAddress.Parse(MulticastAddress);
            udpClient.JoinMulticastGroup(multicastIp);

            _logger.LogInformation("mDNS Responder successfully bound to 0.0.0.0:5353 and joined group 224.0.0.251.");

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    var result = await udpClient.ReceiveAsync(stoppingToken);
                    ProcessMDnsPacket(udpClient, result.Buffer, result.RemoteEndPoint);
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

    private void ProcessMDnsPacket(UdpClient udpClient, byte[] packet, IPEndPoint remoteEndPoint)
    {
        if (packet.Length < 12) return;

        // Check if query (Flags standard query, QR bit = 0)
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
                string.Equals(name, ServiceName, StringComparison.OrdinalIgnoreCase) ||
                string.Equals(name, "lanflix", StringComparison.OrdinalIgnoreCase))
            {
                _logger.LogInformation("Received mDNS query for {QueryName} from {ClientIP}", name, remoteEndPoint.Address);
                SendMDnsResponse(udpClient, remoteEndPoint, type);
            }
        }
    }

    private void SendMDnsResponse(UdpClient udpClient, IPEndPoint targetEndPoint, ushort queryType)
    {
        try
        {
            var localIp = GetLocalIPv4Address();
            if (localIp == null) return;

            var responseBytes = BuildMDnsResponsePacket(localIp, DefaultServerPort);
            var multicastEndPoint = new IPEndPoint(IPAddress.Parse(MulticastAddress), MDnsPort);

            udpClient.Send(responseBytes, responseBytes.Length, multicastEndPoint);
        }
        catch (Exception ex)
        {
            _logger.LogDebug(ex, "Failed to send mDNS response packet");
        }
    }

    private static byte[] BuildMDnsResponsePacket(IPAddress ipAddress, int port)
    {
        using var ms = new MemoryStream();
        using var bw = new BinaryWriter(ms);

        // Transaction ID (0 for mDNS)
        bw.Write((byte)0); bw.Write((byte)0);
        // Flags: Standard query response, Authoritative, No error (0x8400)
        bw.Write((byte)0x84); bw.Write((byte)0x00);
        // Questions count = 0
        bw.Write((byte)0); bw.Write((byte)0);
        // Answer RRs count = 2 (PTR, A)
        bw.Write((byte)0); bw.Write((byte)2);
        // Authority RRs = 0
        bw.Write((byte)0); bw.Write((byte)0);
        // Additional RRs = 0
        bw.Write((byte)0); bw.Write((byte)0);

        // Answer 1: PTR Record for Service
        WriteDnsName(bw, ServiceName);
        bw.Write((byte)0); bw.Write((byte)12); // Type PTR
        bw.Write((byte)0); bw.Write((byte)1);  // Class IN
        bw.Write((byte)0); bw.Write((byte)0); bw.Write((byte)0x00); bw.Write((byte)120); // TTL 120s
        var targetNameBytes = EncodeDnsName(HostName);
        bw.Write((byte)0); bw.Write((byte)targetNameBytes.Length);
        bw.Write(targetNameBytes);

        // Answer 2: A Record for Host
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

    private static IPAddress? GetLocalIPv4Address()
    {
        return NetworkInterface.GetAllNetworkInterfaces()
            .Where(ni => ni.OperationalStatus == OperationalStatus.Up &&
                         ni.NetworkInterfaceType != NetworkInterfaceType.Loopback)
            .SelectMany(ni => ni.GetIPProperties().UnicastAddresses)
            .FirstOrDefault(ua => ua.Address.AddressFamily == AddressFamily.InterNetwork && !IPAddress.IsLoopback(ua.Address))
            ?.Address;
    }
}

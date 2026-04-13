using PacketDotNet;
using SharpPcap;
using NetSentinel.Models;

namespace NetSentinel.Services
{
    /// <summary>
    /// Captures live network packets using SharpPcap/Npcap.
    /// Requires admin/root privileges and Npcap installed on Windows.
    /// Install Npcap from: https://npcap.com/
    /// </summary>
    public class PacketCaptureService : IDisposable
    {
        private readonly ILogger<PacketCaptureService> _logger;
        private ICaptureDevice? _device;
        private bool _isCapturing = false;
        private readonly object _lock = new();

        // In-memory traffic stats (rolling 30-second window)
        private readonly List<(DateTime Time, long Bytes, TrafficProtocol Protocol)> _trafficWindow = new();
        private readonly List<string> _liveLogs = new();
        private const int MaxLiveLogs = 100;

        public event Action<TrafficRecord>? OnPacketCaptured;

        public PacketCaptureService(ILogger<PacketCaptureService> logger)
        {
            _logger = logger;
        }

        public List<string> GetAvailableInterfaces()
        {
            try
            {
                var devices = CaptureDeviceList.Instance;
                return devices.Select(d => $"{d.Name} — {d.Description}").ToList();
            }
            catch (Exception ex)
            {
                _logger.LogWarning("Could not enumerate interfaces: {Error}", ex.Message);
                return new List<string> { "No interfaces found. Ensure Npcap/libpcap is installed." };
            }
        }

        public bool StartCapture(int deviceIndex = 0)
        {
            lock (_lock)
            {
                if (_isCapturing) return false;

                try
                {
                    var devices = CaptureDeviceList.Instance;
                    if (devices.Count == 0)
                    {
                        _logger.LogWarning("No capture devices found. Install Npcap (Windows) or libpcap (Linux).");
                        return false;
                    }

                    _device = devices[deviceIndex];
                    _device.OnPacketArrival += OnPacketArrival;
                    _device.Open(DeviceModes.Promiscuous, 1000);
                    _device.StartCapture();
                    _isCapturing = true;
                    _logger.LogInformation("Packet capture started on: {Device}", _device.Description);
                    return true;
                }
                catch (Exception ex)
                {
                    _logger.LogError("Failed to start capture: {Error}", ex.Message);
                    return false;
                }
            }
        }

        public void StopCapture()
        {
            lock (_lock)
            {
                if (!_isCapturing || _device == null) return;
                _device.StopCapture();
                _device.Close();
                _isCapturing = false;
                _logger.LogInformation("Packet capture stopped.");
            }
        }

        public bool IsCapturing => _isCapturing;

        private void OnPacketArrival(object sender, PacketCapture e)
        {
            try
            {
                var rawPacket = e.GetPacket();
                var packet = Packet.ParsePacket(rawPacket.LinkLayerType, rawPacket.Data);
                var ipPacket = packet.Extract<IPPacket>();

                if (ipPacket == null) return;

                var srcIp = ipPacket.SourceAddress.ToString();
                var dstIp = ipPacket.DestinationAddress.ToString();
                var bytes = rawPacket.Data.Length;
                var protocol = DetermineProtocol(ipPacket);
                var now = DateTime.UtcNow;

                // Record in rolling window
                lock (_trafficWindow)
                {
                    _trafficWindow.Add((now, bytes, protocol));
                    // Clean entries older than 30s
                    var cutoff = now.AddSeconds(-30);
                    _trafficWindow.RemoveAll(t => t.Time < cutoff);
                }

                // Build log line
                var log = BuildLogLine(protocol, srcIp, dstIp, ipPacket, bytes, now);
                lock (_liveLogs)
                {
                    _liveLogs.Insert(0, log);
                    if (_liveLogs.Count > MaxLiveLogs) _liveLogs.RemoveAt(_liveLogs.Count - 1);
                }

                var record = new TrafficRecord
                {
                    SourceIp = srcIp,
                    DestinationIp = dstIp,
                    Protocol = protocol,
                    BytesSent = bytes,
                    Timestamp = now,
                    Details = log
                };

                OnPacketCaptured?.Invoke(record);
            }
            catch (Exception ex)
            {
                _logger.LogDebug("Packet parse error: {Error}", ex.Message);
            }
        }

        private static TrafficProtocol DetermineProtocol(IPPacket ipPacket)
        {
            var tcp = ipPacket.Extract<TcpPacket>();
            if (tcp != null)
            {
                if (tcp.DestinationPort == 80 || tcp.SourcePort == 80 ||
                    tcp.DestinationPort == 443 || tcp.SourcePort == 443)
                    return TrafficProtocol.HTTP;
                return TrafficProtocol.TCP;
            }

            var udp = ipPacket.Extract<UdpPacket>();
            if (udp != null)
            {
                if (udp.DestinationPort == 53 || udp.SourcePort == 53)
                    return TrafficProtocol.DNS;
                return TrafficProtocol.UDP;
            }

            var icmp = ipPacket.Extract<IcmpV4Packet>();
            if (icmp != null) return TrafficProtocol.ICMP;

            return TrafficProtocol.Other;
        }

        private static string BuildLogLine(TrafficProtocol protocol, string src, string dst,
            IPPacket ipPacket, int bytes, DateTime now)
        {
            var tcp = ipPacket.Extract<TcpPacket>();
            var udp = ipPacket.Extract<UdpPacket>();
            var time = now.ToString("HH:mm:ss");

            return protocol switch
            {
                TrafficProtocol.HTTP => $"[{time}] [HTTP] {src}:{tcp?.SourcePort} → {dst}:{tcp?.DestinationPort} ({bytes}B)",
                TrafficProtocol.DNS => $"[{time}] [DNS] {src} → {dst} ({bytes}B)",
                TrafficProtocol.TCP => $"[{time}] [TCP] {src}:{tcp?.SourcePort} → {dst}:{tcp?.DestinationPort} ({bytes}B)",
                TrafficProtocol.UDP => $"[{time}] [UDP] {src}:{udp?.SourcePort} → {dst}:{udp?.DestinationPort} ({bytes}B)",
                TrafficProtocol.ICMP => $"[{time}] [ICMP] {src} → {dst} ({bytes}B)",
                _ => $"[{time}] [OTHER] {src} → {dst} ({bytes}B)"
            };
        }

        public TrafficSummary GetTrafficSummary()
        {
            lock (_trafficWindow)
            {
                var now = DateTime.UtcNow;
                var window = _trafficWindow.Where(t => t.Time >= now.AddSeconds(-5)).ToList();
                double ToMbps(long b) => b / (1024.0 * 1024.0);

                var httpBytes = window.Where(t => t.Protocol == TrafficProtocol.HTTP).Sum(t => t.Bytes);
                var dnsBytes = window.Where(t => t.Protocol == TrafficProtocol.DNS).Sum(t => t.Bytes);
                var otherBytes = window.Where(t => t.Protocol != TrafficProtocol.HTTP
                    && t.Protocol != TrafficProtocol.DNS).Sum(t => t.Bytes);

                // Wave: 25 columns, each = 1 second bucket over last 25s
                var waveData = Enumerable.Range(0, 25).Select(i =>
                {
                    var bucketStart = now.AddSeconds(-(25 - i));
                    var bucketEnd = bucketStart.AddSeconds(1);
                    return (int)(_trafficWindow.Where(t => t.Time >= bucketStart && t.Time < bucketEnd)
                        .Sum(t => t.Bytes) / 1024); // in KB
                }).ToList();

                var total = httpBytes + dnsBytes + otherBytes;
                return new TrafficSummary
                {
                    HttpMbps = Math.Round(ToMbps(httpBytes), 2),
                    DnsMbps = Math.Round(ToMbps(dnsBytes), 2),
                    OtherMbps = Math.Round(ToMbps(otherBytes), 2),
                    TotalMbps = Math.Round(ToMbps(total), 2),
                    WaveData = waveData,
                    Timestamp = now
                };
            }
        }

        public List<string> GetLiveLogs(int count = 20)
        {
            lock (_liveLogs)
            {
                return _liveLogs.Take(count).ToList();
            }
        }

        public void Dispose()
        {
            StopCapture();
            _device?.Dispose();
        }
    }
}

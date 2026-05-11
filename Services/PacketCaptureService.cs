using PacketDotNet;
using SharpPcap;
using NetSentinel.Models;

namespace NetSentinel.Services
{
    /// <summary>
    /// Live packet capture via SharpPcap.
    /// Windows: Install Npcap from https://npcap.com (enable WinPcap-compatible mode). Run app as Administrator.
    /// Linux: sudo apt install libpcap-dev  |  run with sudo or set CAP_NET_RAW capability.
    /// </summary>
    public class PacketCaptureService : IDisposable
    {
        private readonly ILogger<PacketCaptureService> _logger;
        private ICaptureDevice? _device;
        private readonly object _lock = new();
        private readonly List<(DateTime Time, long Bytes, TrafficProtocol Protocol)> _window = new();
        private readonly List<string> _liveLogs = new();
        private const int MaxLogs = 100;

        public bool IsCapturing { get; private set; }
        public event Action<TrafficRecord>? OnPacketCaptured;

        public PacketCaptureService(ILogger<PacketCaptureService> logger) => _logger = logger;

        public List<string> GetAvailableInterfaces()
        {
            try { return CaptureDeviceList.Instance.Select(d => $"{d.Name} — {d.Description}").ToList(); }
            catch { return new List<string> { "No interfaces. Install Npcap (Windows) or libpcap (Linux)." }; }
        }

        public bool StartCapture(int deviceIndex = 0)
        {
            lock (_lock)
            {
                if (IsCapturing) return false;
                try
                {
                    var devices = CaptureDeviceList.Instance;
                    if (devices.Count == 0) { _logger.LogWarning("No capture devices found."); return false; }
                    _device = devices[deviceIndex];
                    _device.OnPacketArrival += OnArrival;
                    _device.Open(DeviceModes.Promiscuous, 1000);
                    _device.StartCapture();
                    IsCapturing = true;
                    _logger.LogInformation("Capture started: {Dev}", _device.Description);
                    return true;
                }
                catch (Exception ex) { _logger.LogError("Capture start failed: {Err}", ex.Message); return false; }
            }
        }

        public void StopCapture()
        {
            lock (_lock)
            {
                if (!IsCapturing || _device == null) return;
                _device.StopCapture();
                _device.Close();
                IsCapturing = false;
            }
        }

        private void OnArrival(object sender, PacketCapture e)
        {
            try
            {
                var raw = e.GetPacket();
                var pkt = Packet.ParsePacket(raw.LinkLayerType, raw.Data);
                var ip = pkt.Extract<IPPacket>();
                if (ip == null) return;

                var src = ip.SourceAddress.ToString();
                var dst = ip.DestinationAddress.ToString();
                var bytes = raw.Data.Length;
                var proto = GetProtocol(ip);
                var now = DateTime.UtcNow;
                var logLine = BuildLog(proto, src, dst, ip, bytes, now);

                lock (_window)
                {
                    _window.Add((now, bytes, proto));
                    _window.RemoveAll(t => t.Time < now.AddSeconds(-30));
                }
                lock (_liveLogs)
                {
                    _liveLogs.Insert(0, logLine);
                    if (_liveLogs.Count > MaxLogs) _liveLogs.RemoveAt(_liveLogs.Count - 1);
                }

                OnPacketCaptured?.Invoke(new TrafficRecord
                {
                    SourceIp = src, DestinationIp = dst,
                    SourcePort = ip.Extract<TcpPacket>()?.SourcePort ?? ip.Extract<UdpPacket>()?.SourcePort ?? 0,
                    DestinationPort = ip.Extract<TcpPacket>()?.DestinationPort ?? ip.Extract<UdpPacket>()?.DestinationPort ?? 0,
                    Protocol = proto, BytesSent = bytes, Timestamp = now, Details = logLine
                });
            }
            catch { }
        }

        private static TrafficProtocol GetProtocol(IPPacket ip)
        {
            var tcp = ip.Extract<TcpPacket>();
            if (tcp != null)
                return (tcp.DestinationPort == 80 || tcp.SourcePort == 80 ||
                        tcp.DestinationPort == 443 || tcp.SourcePort == 443)
                    ? TrafficProtocol.HTTP : TrafficProtocol.TCP;
            var udp = ip.Extract<UdpPacket>();
            if (udp != null)
                return (udp.DestinationPort == 53 || udp.SourcePort == 53) ? TrafficProtocol.DNS : TrafficProtocol.UDP;
            if (ip.Extract<IcmpV4Packet>() != null) return TrafficProtocol.ICMP;
            return TrafficProtocol.Other;
        }

        private static string BuildLog(TrafficProtocol proto, string src, string dst, IPPacket ip, int bytes, DateTime now)
        {
            var tcp = ip.Extract<TcpPacket>();
            var udp = ip.Extract<UdpPacket>();
            var t = now.ToString("HH:mm:ss");
            return proto switch
            {
                TrafficProtocol.HTTP => $"[{t}] [HTTP] {src}:{tcp?.SourcePort} → {dst}:{tcp?.DestinationPort} ({bytes}B)",
                TrafficProtocol.DNS  => $"[{t}] [DNS]  {src} → {dst} ({bytes}B)",
                TrafficProtocol.TCP  => $"[{t}] [TCP]  {src}:{tcp?.SourcePort} → {dst}:{tcp?.DestinationPort} ({bytes}B)",
                TrafficProtocol.UDP  => $"[{t}] [UDP]  {src}:{udp?.SourcePort} → {dst}:{udp?.DestinationPort} ({bytes}B)",
                TrafficProtocol.ICMP => $"[{t}] [ICMP] {src} → {dst} ({bytes}B)",
                _                    => $"[{t}] [???]  {src} → {dst} ({bytes}B)"
            };
        }

        public TrafficSummary GetTrafficSummary()
        {
            lock (_window)
            {
                var now = DateTime.UtcNow;
                var recent = _window.Where(t => t.Time >= now.AddSeconds(-5)).ToList();
                double ToMb(long b) => b / (1024.0 * 1024.0);
                var http  = recent.Where(t => t.Protocol == TrafficProtocol.HTTP).Sum(t => t.Bytes);
                var dns   = recent.Where(t => t.Protocol == TrafficProtocol.DNS).Sum(t => t.Bytes);
                var other = recent.Where(t => t.Protocol != TrafficProtocol.HTTP && t.Protocol != TrafficProtocol.DNS).Sum(t => t.Bytes);
                var wave  = Enumerable.Range(0, 25).Select(i =>
                {
                    var s = now.AddSeconds(-(25 - i)); var e2 = s.AddSeconds(1);
                    return (int)(_window.Where(t => t.Time >= s && t.Time < e2).Sum(t => t.Bytes) / 1024);
                }).ToList();
                return new TrafficSummary
                {
                    HttpMbps = Math.Round(ToMb(http), 2), DnsMbps = Math.Round(ToMb(dns), 2),
                    OtherMbps = Math.Round(ToMb(other), 2), TotalMbps = Math.Round(ToMb(http + dns + other), 2),
                    WaveData = wave
                };
            }
        }

        public List<string> GetLiveLogs(int count = 20) { lock (_liveLogs) return _liveLogs.Take(count).ToList(); }

        public void Dispose() { StopCapture(); _device?.Dispose(); }
    }
}

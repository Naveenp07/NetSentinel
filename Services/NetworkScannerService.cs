using System.Diagnostics;
using System.Net;
using System.Net.NetworkInformation;
using NetSentinel.Data;
using NetSentinel.Models;
 
namespace NetSentinel.Services
{
    public class NetworkScannerService
    {
        private readonly AppDbContext _db;
        private readonly IConfiguration _config;
        private readonly ILogger<NetworkScannerService> _logger;
 
        public NetworkScannerService(AppDbContext db, IConfiguration config,
            ILogger<NetworkScannerService> logger)
        {
            _db = db;
            _config = config;
            _logger = logger;
        }
 
        /// <summary>
        /// Auto-detects the local subnet base (e.g. "192.168.0") from active interfaces.
        /// </summary>
        public static string DetectLocalSubnet()
        {
            foreach (var ni in NetworkInterface.GetAllNetworkInterfaces())
            {
                if (ni.OperationalStatus != OperationalStatus.Up) continue;
                if (ni.NetworkInterfaceType == NetworkInterfaceType.Loopback) continue;
 
                foreach (var addr in ni.GetIPProperties().UnicastAddresses)
                {
                    if (addr.Address.AddressFamily != System.Net.Sockets.AddressFamily.InterNetwork)
                        continue;
                    var ip = addr.Address.ToString();
                    if (ip.StartsWith("127.") || ip.StartsWith("169.254.")) continue;
                    var parts = ip.Split('.');
                    if (parts.Length == 4)
                        return $"{parts[0]}.{parts[1]}.{parts[2]}";
                }
            }
            return "192.168.1";
        }
 
        public async Task<ScanResult> ScanNetworkAsync()
        {
            var sw = Stopwatch.StartNew();
 
            // Auto-detect subnet if not configured or still default
            var subnetBase = _config["NetSentinel:SubnetBase"] ?? "";
            if (string.IsNullOrEmpty(subnetBase) || subnetBase == "192.168.1")
                subnetBase = DetectLocalSubnet();
 
            var start = int.TryParse(_config["NetSentinel:ScanRangeStart"], out var s) ? s : 1;
            var end = int.TryParse(_config["NetSentinel:ScanRangeEnd"], out var e) ? e : 254;
            var timeout = int.TryParse(_config["NetSentinel:PingTimeoutMs"], out var t) ? t : 800;
 
            _logger.LogInformation("Scanning {Subnet}.{Start}-{End} timeout={Timeout}ms",
                subnetBase, start, end, timeout);
 
            // Ping entire range in parallel
            var tasks = Enumerable.Range(start, end - start + 1)
                .Select(i => PingHostAsync($"{subnetBase}.{i}", timeout))
                .ToList();
 
            var results = await Task.WhenAll(tasks);
            var onlineHosts = results.Where(r => r != null).Cast<Device>().ToList();
 
            _logger.LogInformation("{Count} hosts responded to ping", onlineHosts.Count);
 
            // Read full ARP table once (fast, single process call)
            var arpTable = GetFullArpTable();
 
            int newDevices = 0;
 
            foreach (var host in onlineHosts)
            {
                if (arpTable.TryGetValue(host.IpAddress, out var mac))
                    host.MacAddress = mac;
 
                var existing = _db.Devices.FirstOrDefault(d => d.IpAddress == host.IpAddress);
                if (existing == null)
                {
                    newDevices++;
                    _db.Devices.Add(host);
                    _logger.LogInformation("NEW: {IP} ({MAC}) [{Hostname}]",
                        host.IpAddress, host.MacAddress, host.Hostname);
                    await LogEntryAsync("INFO",
                        $"New device: {host.IpAddress} MAC={host.MacAddress} ({host.Hostname})",
                        "Scanner");
                }
                else
                {
                    existing.IsOnline = true;
                    existing.LastSeen = DateTime.UtcNow;
                    if (host.MacAddress != "Unknown") existing.MacAddress = host.MacAddress;
                    if (!string.IsNullOrEmpty(host.Hostname) && host.Hostname != "Unknown")
                        existing.Hostname = host.Hostname;
                }
            }
 
            // Mark devices not seen this scan as offline
            var onlineIps = onlineHosts.Select(h => h.IpAddress).ToHashSet();
            foreach (var device in _db.Devices.ToList())
            {
                if (!onlineIps.Contains(device.IpAddress) && device.IsOnline)
                {
                    device.IsOnline = false;
                    device.LastOffline = DateTime.UtcNow;
                }
            }
 
            sw.Stop();
 
            var allDevices = _db.Devices.ToList();
            var scanResult = new ScanResult
            {
                DevicesFound = allDevices.Count,
                OnlineCount = onlineHosts.Count,
                NewDevicesFound = newDevices,
                DurationMs = sw.Elapsed.TotalMilliseconds
            };
            _db.ScanResults.Add(scanResult);
            await _db.SaveChangesAsync();
 
            await LogEntryAsync("OK",
                $"Scan done — {onlineHosts.Count} online, {newDevices} new, took {sw.Elapsed.TotalMilliseconds:F0}ms",
                "Scanner");
 
            return scanResult;
        }
 
        private async Task<Device?> PingHostAsync(string ip, int timeout)
        {
            try
            {
                using var ping = new Ping();
                var reply = await ping.SendPingAsync(ip, timeout);
                if (reply.Status == IPStatus.Success)
                {
                    var hostname = "Unknown";
                    try { hostname = (await Dns.GetHostEntryAsync(ip)).HostName; }
                    catch { }
 
                    return new Device
                    {
                        IpAddress = ip,
                        MacAddress = "Unknown",
                        Hostname = hostname,
                        IsOnline = true,
                        FirstSeen = DateTime.UtcNow,
                        LastSeen = DateTime.UtcNow
                    };
                }
            }
            catch (Exception ex)
            {
                _logger.LogDebug("Ping {IP} failed: {Err}", ip, ex.Message);
            }
            return null;
        }
 
        /// <summary>
        /// Reads the full ARP table with one arp -a call and returns IP->MAC map.
        /// Windows line:  192.168.1.1     aa-bb-cc-dd-ee-ff   dynamic
        /// Linux line:    192.168.1.1     ether aa:bb:cc:dd:ee:ff   C eth0
        /// </summary>
        private Dictionary<string, string> GetFullArpTable()
        {
            var table = new Dictionary<string, string>();
            try
            {
                using var proc = Process.Start(new ProcessStartInfo
                {
                    FileName = "arp",
                    Arguments = "-a",
                    RedirectStandardOutput = true,
                    UseShellExecute = false,
                    CreateNoWindow = true
                });
                if (proc == null) return table;
 
                var output = proc.StandardOutput.ReadToEnd();
                proc.WaitForExit(3000);
 
                foreach (var line in output.Split('\n'))
                {
                    var parts = line.Trim().Split(
                        new[] { ' ', '\t' }, StringSplitOptions.RemoveEmptyEntries);
 
                    string? foundIp = null, foundMac = null;
                    foreach (var part in parts)
                    {
                        if (foundIp == null && IsValidIp(part)) foundIp = part;
                        if (foundMac == null && IsMacAddress(part))
                            foundMac = part.ToUpper().Replace('-', ':');
                    }
                    if (foundIp != null && foundMac != null)
                        table[foundIp] = foundMac;
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning("ARP table read failed: {Err}", ex.Message);
            }
            return table;
        }
 
        private static bool IsValidIp(string s)
        {
            var p = s.Split('.');
            return p.Length == 4 && p.All(x => byte.TryParse(x, out _));
        }
 
        private static bool IsMacAddress(string s)
        {
            if (s.Length != 17) return false;
            var sep = s[2];
            if (sep != '-' && sep != ':') return false;
            return s.Replace("-", "").Replace(":", "")
                    .All(c => "0123456789ABCDEFabcdef".Contains(c));
        }
 
        private async Task LogEntryAsync(string level, string message, string source)
        {
            _db.LogEntries.Add(new LogEntry
            {
                Level = level, Message = message,
                Source = source, Timestamp = DateTime.UtcNow
            });
            await _db.SaveChangesAsync();
        }
    }
}
 
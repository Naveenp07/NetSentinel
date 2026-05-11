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

        public NetworkScannerService(AppDbContext db, IConfiguration config, ILogger<NetworkScannerService> logger)
        {
            _db = db;
            _config = config;
            _logger = logger;
        }

        public async Task<ScanResult> ScanNetworkAsync()
        {
            var sw = Stopwatch.StartNew();
            var subnetBase = _config["NetSentinel:SubnetBase"] ?? "192.168.1";
            var start = int.Parse(_config["NetSentinel:ScanRangeStart"] ?? "1");
            var end = int.Parse(_config["NetSentinel:ScanRangeEnd"] ?? "254");
            var timeout = int.Parse(_config["NetSentinel:PingTimeoutMs"] ?? "500");

            _logger.LogInformation("Starting scan on {Subnet}.{Start}-{End}", subnetBase, start, end);

            var tasks = Enumerable.Range(start, end - start + 1)
                .Select(i => PingHostAsync($"{subnetBase}.{i}", timeout))
                .ToList();

            var results = await Task.WhenAll(tasks);
            var onlineHosts = results.Where(r => r != null).Cast<Device>().ToList();

            int newDevices = 0;
            foreach (var host in onlineHosts)
            {
                var existing = _db.Devices.FirstOrDefault(d => d.IpAddress == host.IpAddress);
                if (existing == null)
                {
                    newDevices++;
                    _db.Devices.Add(host);
                    await LogAsync("INFO", $"New device: {host.IpAddress} ({host.MacAddress})", "Scanner");
                }
                else
                {
                    existing.IsOnline = true;
                    existing.LastSeen = DateTime.UtcNow;
                    existing.MacAddress = host.MacAddress;
                    if (!string.IsNullOrEmpty(host.Hostname) && host.Hostname != "Unknown")
                        existing.Hostname = host.Hostname;
                }
            }

            // Mark missing devices offline
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
            await LogAsync("OK", $"Scan complete — {onlineHosts.Count} online, {newDevices} new ({sw.Elapsed.TotalMilliseconds:F0}ms)", "Scanner");
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
                    return new Device
                    {
                        IpAddress = ip,
                        MacAddress = GetMacAddress(ip),
                        Hostname = await ResolveHostnameAsync(ip),
                        IsOnline = true,
                        FirstSeen = DateTime.UtcNow,
                        LastSeen = DateTime.UtcNow
                    };
                }
            }
            catch (Exception ex) { _logger.LogDebug("Ping {IP}: {Err}", ip, ex.Message); }
            return null;
        }

        private static string GetMacAddress(string ip)
        {
            try
            {
                var psi = new ProcessStartInfo
                {
                    FileName = "arp",
                    Arguments = OperatingSystem.IsWindows() ? $"-a {ip}" : $"-n {ip}",
                    RedirectStandardOutput = true,
                    UseShellExecute = false,
                    CreateNoWindow = true
                };
                using var p = Process.Start(psi)!;
                var output = p.StandardOutput.ReadToEnd();
                p.WaitForExit();
                foreach (var line in output.Split('\n'))
                {
                    if (line.Contains(ip))
                    {
                        foreach (var part in line.Trim().Split(new char[] { ' ', '\t' }, StringSplitOptions.RemoveEmptyEntries))
                            if (part.Length >= 17 && (part.Contains('-') || part.Contains(':')))
                                return part.ToUpper();
                    }
                }
            }
            catch { }
            return "Unknown";
        }

        private static async Task<string> ResolveHostnameAsync(string ip)
        {
            try { return (await System.Net.Dns.GetHostEntryAsync(ip)).HostName; }
            catch { return "Unknown"; }
        }

        private async Task LogAsync(string level, string message, string source)
        {
            _db.LogEntries.Add(new LogEntry { Level = level, Message = message, Source = source });
            await _db.SaveChangesAsync();
        }
    }
}

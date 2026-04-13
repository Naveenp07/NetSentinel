using Microsoft.EntityFrameworkCore;
using NetSentinel.Data;
using NetSentinel.Models;

namespace NetSentinel.Services
{
    public class AlertService
    {
        private readonly AppDbContext _db;
        private readonly IConfiguration _config;
        private readonly ILogger<AlertService> _logger;

        public AlertService(AppDbContext db, IConfiguration config,
            ILogger<AlertService> logger)
        {
            _db = db;
            _config = config;
            _logger = logger;
        }

        /// <summary>
        /// Run all alert checks after a scan. Returns new alerts created.
        /// </summary>
        public async Task<List<Alert>> RunChecksAsync()
        {
            var newAlerts = new List<Alert>();

            newAlerts.AddRange(await CheckUnknownDevicesAsync());
            newAlerts.AddRange(await CheckTrafficSpikesAsync());

            return newAlerts;
        }

        /// <summary>
        /// Detect devices not in the known/approved list.
        /// </summary>
        private async Task<List<Alert>> CheckUnknownDevicesAsync()
        {
            var alerts = new List<Alert>();
            var unknownDevices = await _db.Devices
                .Where(d => d.IsOnline && !d.IsKnown)
                .ToListAsync();

            foreach (var device in unknownDevices)
            {
                // Only alert once per device (check if we already have an unack'd alert)
                var exists = await _db.Alerts.AnyAsync(a =>
                    a.Type == AlertType.UnknownDevice &&
                    a.SourceIp == device.IpAddress &&
                    !a.IsAcknowledged);

                if (!exists)
                {
                    var alert = new Alert
                    {
                        Type = AlertType.UnknownDevice,
                        Severity = AlertSeverity.Critical,
                        Message = $"Unknown device connected: {device.IpAddress} (MAC: {device.MacAddress})",
                        SourceIp = device.IpAddress,
                        Timestamp = DateTime.UtcNow
                    };
                    _db.Alerts.Add(alert);
                    alerts.Add(alert);
                    _logger.LogWarning("ALERT: Unknown device at {IP}", device.IpAddress);
                }
            }

            await _db.SaveChangesAsync();
            return alerts;
        }

        /// <summary>
        /// Detect traffic spikes based on bytes sent per IP in last 30 seconds.
        /// </summary>
        private async Task<List<Alert>> CheckTrafficSpikesAsync()
        {
            var alerts = new List<Alert>();
            var thresholdMb = double.Parse(_config["NetSentinel:TrafficSpikeThresholdMB"] ?? "50");
            var thresholdBytes = (long)(thresholdMb * 1024 * 1024);
            var since = DateTime.UtcNow.AddSeconds(-30);

            var trafficByIp = await _db.TrafficRecords
                .Where(t => t.Timestamp >= since)
                .GroupBy(t => t.SourceIp)
                .Select(g => new { Ip = g.Key, TotalBytes = g.Sum(t => t.BytesSent) })
                .ToListAsync();

            foreach (var item in trafficByIp)
            {
                if (item.TotalBytes >= thresholdBytes)
                {
                    var mb = item.TotalBytes / (1024.0 * 1024.0);
                    var alert = new Alert
                    {
                        Type = AlertType.TrafficSpike,
                        Severity = AlertSeverity.Warning,
                        Message = $"Traffic spike: {item.Ip} sent {mb:F1} MB in 30 seconds",
                        SourceIp = item.Ip,
                        Timestamp = DateTime.UtcNow
                    };
                    _db.Alerts.Add(alert);
                    alerts.Add(alert);
                    _logger.LogWarning("ALERT: Traffic spike from {IP} — {MB:F1} MB", item.Ip, mb);
                }
            }

            await _db.SaveChangesAsync();
            return alerts;
        }

        /// <summary>
        /// Flag a suspicious DNS query.
        /// </summary>
        public async Task<Alert> CreateDnsAlertAsync(string sourceIp, string queriedHost)
        {
            var alert = new Alert
            {
                Type = AlertType.SuspiciousDns,
                Severity = AlertSeverity.Warning,
                Message = $"Suspicious DNS query from {sourceIp}: {queriedHost}",
                SourceIp = sourceIp,
                Timestamp = DateTime.UtcNow
            };
            _db.Alerts.Add(alert);
            await _db.SaveChangesAsync();
            return alert;
        }

        /// <summary>
        /// Acknowledge an alert by ID.
        /// </summary>
        public async Task<bool> AcknowledgeAsync(int alertId)
        {
            var alert = await _db.Alerts.FindAsync(alertId);
            if (alert == null) return false;
            alert.IsAcknowledged = true;
            await _db.SaveChangesAsync();
            return true;
        }
    }
}

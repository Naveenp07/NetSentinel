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

        public AlertService(AppDbContext db, IConfiguration config, ILogger<AlertService> logger)
        {
            _db = db; _config = config; _logger = logger;
        }

        public async Task<List<Alert>> RunChecksAsync()
        {
            var alerts = new List<Alert>();
            alerts.AddRange(await CheckUnknownDevicesAsync());
            alerts.AddRange(await CheckTrafficSpikesAsync());
            return alerts;
        }

        private async Task<List<Alert>> CheckUnknownDevicesAsync()
        {
            var alerts = new List<Alert>();
            var unknown = await _db.Devices.Where(d => d.IsOnline && !d.IsKnown).ToListAsync();
            foreach (var device in unknown)
            {
                var exists = await _db.Alerts.AnyAsync(a =>
                    a.Type == AlertType.UnknownDevice && a.SourceIp == device.IpAddress && !a.IsAcknowledged);
                if (!exists)
                {
                    var alert = new Alert
                    {
                        Type = AlertType.UnknownDevice,
                        Severity = AlertSeverity.Critical,
                        Message = $"Unknown device connected: {device.IpAddress} (MAC: {device.MacAddress})",
                        SourceIp = device.IpAddress
                    };
                    _db.Alerts.Add(alert);
                    alerts.Add(alert);
                    _logger.LogWarning("ALERT: Unknown device at {IP}", device.IpAddress);
                }
            }
            await _db.SaveChangesAsync();
            return alerts;
        }

        private async Task<List<Alert>> CheckTrafficSpikesAsync()
        {
            var alerts = new List<Alert>();
            var thresholdBytes = (long)(double.Parse(_config["NetSentinel:TrafficSpikeThresholdMB"] ?? "50") * 1024 * 1024);
            var since = DateTime.UtcNow.AddSeconds(-30);
            var traffic = await _db.TrafficRecords
                .Where(t => t.Timestamp >= since)
                .GroupBy(t => t.SourceIp)
                .Select(g => new { Ip = g.Key, Total = g.Sum(t => t.BytesSent) })
                .ToListAsync();
            foreach (var item in traffic.Where(i => i.Total >= thresholdBytes))
            {
                var mb = item.Total / (1024.0 * 1024.0);
                var alert = new Alert
                {
                    Type = AlertType.TrafficSpike,
                    Severity = AlertSeverity.Warning,
                    Message = $"Traffic spike: {item.Ip} sent {mb:F1} MB in 30 seconds",
                    SourceIp = item.Ip
                };
                _db.Alerts.Add(alert);
                alerts.Add(alert);
            }
            await _db.SaveChangesAsync();
            return alerts;
        }

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

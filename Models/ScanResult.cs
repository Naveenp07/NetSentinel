using System.ComponentModel.DataAnnotations;

namespace NetSentinel.Models
{
    public class ScanResult
    {
        public int Id { get; set; }

        public DateTime ScanTime { get; set; } = DateTime.UtcNow;

        public int DevicesFound { get; set; }

        public int OnlineCount { get; set; }

        public int NewDevicesFound { get; set; }

        public double DurationMs { get; set; }
    }

    public class LogEntry
    {
        public int Id { get; set; }

        [MaxLength(10)]
        public string Level { get; set; } = "INFO";

        [MaxLength(500)]
        public string Message { get; set; } = string.Empty;

        [MaxLength(50)]
        public string? Source { get; set; }

        public DateTime Timestamp { get; set; } = DateTime.UtcNow;
    }

    public class DashboardStats
    {
        public int TotalDevices { get; set; }
        public int OnlineDevices { get; set; }
        public int OfflineDevices { get; set; }
        public int ActiveAlerts { get; set; }
        public double CurrentTrafficMbps { get; set; }
        public bool TrafficSpike { get; set; }
        public DateTime LastScan { get; set; }
    }
}

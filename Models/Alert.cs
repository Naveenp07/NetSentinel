using System.ComponentModel.DataAnnotations;
namespace NetSentinel.Models {
    public enum AlertSeverity { Info, Warning, Critical }
    public enum AlertType { UnknownDevice, TrafficSpike, DeviceOffline, DeviceOnline, SuspiciousDns, PortScan }
    public class Alert {
        public int Id { get; set; }
        public AlertType Type { get; set; }
        public AlertSeverity Severity { get; set; }
        [Required][MaxLength(200)] public string Message { get; set; } = string.Empty;
        [MaxLength(50)] public string? SourceIp { get; set; }
        [MaxLength(50)] public string? DestinationIp { get; set; }
        public bool IsAcknowledged { get; set; } = false;
        public DateTime Timestamp { get; set; } = DateTime.UtcNow;
    }
}

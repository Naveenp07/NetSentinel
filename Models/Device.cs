using System.ComponentModel.DataAnnotations;
namespace NetSentinel.Models {
    public class Device {
        public int Id { get; set; }
        [Required][MaxLength(50)] public string IpAddress { get; set; } = string.Empty;
        [MaxLength(20)] public string MacAddress { get; set; } = "Unknown";
        [MaxLength(100)] public string Hostname { get; set; } = "Unknown";
        public bool IsOnline { get; set; }
        public bool IsKnown { get; set; } = false;
        [MaxLength(100)] public string? DeviceLabel { get; set; }
        public DateTime FirstSeen { get; set; } = DateTime.UtcNow;
        public DateTime LastSeen { get; set; } = DateTime.UtcNow;
        public DateTime? LastOffline { get; set; }
    }
}

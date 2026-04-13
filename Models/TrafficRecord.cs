using System.ComponentModel.DataAnnotations;

namespace NetSentinel.Models
{
    public enum TrafficProtocol
    {
        HTTP,
        DNS,
        TCP,
        UDP,
        ICMP,
        Other
    }

    public class TrafficRecord
    {
        public int Id { get; set; }

        [MaxLength(50)]
        public string SourceIp { get; set; } = string.Empty;

        [MaxLength(50)]
        public string DestinationIp { get; set; } = string.Empty;

        public int SourcePort { get; set; }

        public int DestinationPort { get; set; }

        public TrafficProtocol Protocol { get; set; }

        public long BytesSent { get; set; }

        public DateTime Timestamp { get; set; } = DateTime.UtcNow;

        [MaxLength(500)]
        public string? Details { get; set; }
    }

    public class TrafficSummary
    {
        public double HttpMbps { get; set; }
        public double DnsMbps { get; set; }
        public double OtherMbps { get; set; }
        public double TotalMbps { get; set; }
        public List<int> WaveData { get; set; } = new();
        public DateTime Timestamp { get; set; } = DateTime.UtcNow;
    }
}

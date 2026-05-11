using Microsoft.EntityFrameworkCore;
using NetSentinel.Models;

namespace NetSentinel.Data
{
    public class AppDbContext : DbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

        public DbSet<Device> Devices { get; set; }
        public DbSet<Alert> Alerts { get; set; }
        public DbSet<TrafficRecord> TrafficRecords { get; set; }
        public DbSet<ScanResult> ScanResults { get; set; }
        public DbSet<LogEntry> LogEntries { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            // Device
            modelBuilder.Entity<Device>(e =>
            {
                e.ToTable("Devices");
                e.HasKey(d => d.Id);
                e.Property(d => d.IpAddress).HasMaxLength(50).IsRequired();
                e.Property(d => d.MacAddress).HasMaxLength(20).HasDefaultValue("Unknown");
                e.Property(d => d.Hostname).HasMaxLength(100).HasDefaultValue("Unknown");
                e.Property(d => d.DeviceLabel).HasMaxLength(100);
                e.Property(d => d.FirstSeen).HasDefaultValueSql("GETUTCDATE()");
                e.Property(d => d.LastSeen).HasDefaultValueSql("GETUTCDATE()");
                e.HasIndex(d => d.IpAddress).IsUnique();
                e.HasIndex(d => d.MacAddress);
                e.HasIndex(d => d.IsOnline);
            });

            // Alert
            modelBuilder.Entity<Alert>(e =>
            {
                e.ToTable("Alerts");
                e.HasKey(a => a.Id);
                e.Property(a => a.Message).HasMaxLength(200).IsRequired();
                e.Property(a => a.SourceIp).HasMaxLength(50);
                e.Property(a => a.DestinationIp).HasMaxLength(50);
                e.Property(a => a.Timestamp).HasDefaultValueSql("GETUTCDATE()");
                e.Property(a => a.Type).HasConversion<string>().HasMaxLength(30);
                e.Property(a => a.Severity).HasConversion<string>().HasMaxLength(15);
                e.HasIndex(a => a.Timestamp);
                e.HasIndex(a => a.IsAcknowledged);
                e.HasIndex(a => new { a.SourceIp, a.Type, a.IsAcknowledged });
            });

            // TrafficRecord
            modelBuilder.Entity<TrafficRecord>(e =>
            {
                e.ToTable("TrafficRecords");
                e.HasKey(t => t.Id);
                e.Property(t => t.SourceIp).HasMaxLength(50);
                e.Property(t => t.DestinationIp).HasMaxLength(50);
                e.Property(t => t.Details).HasMaxLength(500);
                e.Property(t => t.Timestamp).HasDefaultValueSql("GETUTCDATE()");
                e.Property(t => t.Protocol).HasConversion<string>().HasMaxLength(10);
                e.HasIndex(t => t.Timestamp);
                e.HasIndex(t => t.SourceIp);
                e.HasIndex(t => t.Protocol);
            });

            // ScanResult
            modelBuilder.Entity<ScanResult>(e =>
            {
                e.ToTable("ScanResults");
                e.HasKey(s => s.Id);
                e.Property(s => s.ScanTime).HasDefaultValueSql("GETUTCDATE()");
                e.HasIndex(s => s.ScanTime);
            });

            // LogEntry
            modelBuilder.Entity<LogEntry>(e =>
            {
                e.ToTable("LogEntries");
                e.HasKey(l => l.Id);
                e.Property(l => l.Level).HasMaxLength(10).HasDefaultValue("INFO");
                e.Property(l => l.Message).HasMaxLength(500).IsRequired();
                e.Property(l => l.Source).HasMaxLength(50);
                e.Property(l => l.Timestamp).HasDefaultValueSql("GETUTCDATE()");
                e.HasIndex(l => l.Timestamp);
                e.HasIndex(l => l.Level);
            });
        }
    }
}

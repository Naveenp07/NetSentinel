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
            modelBuilder.Entity<Device>()
                .HasIndex(d => d.IpAddress)
                .IsUnique();

            modelBuilder.Entity<Device>()
                .HasIndex(d => d.MacAddress);

            modelBuilder.Entity<Alert>()
                .HasIndex(a => a.Timestamp);

            modelBuilder.Entity<TrafficRecord>()
                .HasIndex(t => t.Timestamp);

            modelBuilder.Entity<LogEntry>()
                .HasIndex(l => l.Timestamp);
        }
    }
}

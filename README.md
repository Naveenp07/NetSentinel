# 🛡️ NetSentinel

Real-time local network monitor — **C# / ASP.NET Core 8**, **SQL Server 2019**, **SignalR**, **SharpPcap**.

---

## ⚙️ Requirements

| Tool | Version | Notes |
|------|---------|-------|
| .NET SDK | 8.0+ | https://dotnet.microsoft.com/download |
| SQL Server | 2019 | Express edition is free |
| Npcap | Latest | Windows only — for packet capture |

### Install SQL Server 2019 (Express — Free)
Download: https://www.microsoft.com/en-us/sql-server/sql-server-downloads

After install, enable **TCP/IP** in SQL Server Configuration Manager → Port 1433.

---

## 🚀 Quick Start

### 1. Update Connection String
Open `appsettings.json` and set your SQL Server credentials:

```json
"ConnectionStrings": {
  "DefaultConnection": "Server=localhost,1433;Database=NetSentinelDB;User Id=sa;Password=YourStrong@Password123;TrustServerCertificate=True;"
}
```

If using **Windows Authentication** instead of SQL login:
```json
"DefaultConnection": "Server=localhost;Database=NetSentinelDB;Integrated Security=True;TrustServerCertificate=True;"
```

### 2. Update Subnet
In `appsettings.json`, set your local subnet:
```json
"SubnetBase": "192.168.1"   // change to match your network e.g. 10.0.0
```

### 3. Run

```bash
# Restore packages
dotnet restore

# Apply database migrations (creates NetSentinelDB automatically)
dotnet ef database update

# Run (as Administrator on Windows for packet capture)
dotnet run
```

Open: **http://localhost:5000**

---

## 🗄️ Database Setup (SQL Server 2019)

The app runs `db.Database.Migrate()` on startup — it creates the database and all tables automatically.

To run migrations manually:
```bash
# Create migration (after model changes)
dotnet ef migrations add YourMigrationName

# Apply to database
dotnet ef database update

# Rollback to specific migration
dotnet ef database update PreviousMigrationName
```

### Tables Created

| Table | Description |
|-------|-------------|
| `Devices` | Discovered network devices |
| `Alerts` | Security alerts (unknown devices, spikes) |
| `TrafficRecords` | Per-packet traffic log |
| `ScanResults` | History of network scans |
| `LogEntries` | Application log |

---

## 📡 Packet Capture Setup

### Windows
1. Download **Npcap**: https://npcap.com/
2. During install: ✅ check **"Install Npcap in WinPcap API-compatible mode"**
3. Run NetSentinel **as Administrator**
4. Call `POST /api/traffic/capture/start` to begin capturing

### Linux / macOS
```bash
sudo apt install libpcap-dev       # Ubuntu/Debian
sudo yum install libpcap-devel     # RHEL/CentOS

# Run with elevated privileges
sudo dotnet run

# Or set capability (no sudo needed after this)
sudo setcap cap_net_raw=eip ./NetSentinel
```

---

## 🔌 API Reference

### Devices
```
GET    /api/devices              List all devices
POST   /api/devices/scan         Trigger network scan
PUT    /api/devices/{id}/trust   Mark device as trusted/known
PUT    /api/devices/{id}/label   Set custom label
DELETE /api/devices/{id}         Remove device
```

### Alerts
```
GET  /api/alerts                          All alerts
GET  /api/alerts?unacknowledgedOnly=true  Active alerts only
GET  /api/alerts/count                    Unacknowledged count
POST /api/alerts/{id}/acknowledge         Acknowledge one
POST /api/alerts/acknowledge-all          Acknowledge all
```

### Traffic
```
GET  /api/traffic/stats           Live traffic summary (MB/s)
GET  /api/traffic/logs            Recent packet log lines
GET  /api/traffic/interfaces      Available capture interfaces
POST /api/traffic/capture/start   Start packet capture
POST /api/traffic/capture/stop    Stop packet capture
GET  /api/traffic/by-protocol     Traffic grouped by protocol
GET  /api/traffic/history         Historical records
```

---

## 📁 Project Structure

```
NetSentinel/
├── Controllers/
│   ├── DevicesController.cs       API: scan + device CRUD
│   ├── AlertsController.cs        API: alert management
│   ├── TrafficController.cs       API: traffic + capture
│   └── HomeController.cs          Dashboard + Logs views
├── Data/
│   └── AppDbContext.cs            EF Core — SQL Server 2019
├── Hubs/
│   └── NetworkHub.cs              SignalR real-time push
├── Migrations/
│   └── 20240101000000_InitialCreate.cs
├── Models/
│   ├── Device.cs
│   ├── Alert.cs
│   ├── TrafficRecord.cs
│   └── ScanResult.cs
├── Services/
│   ├── NetworkScannerService.cs   Ping sweep + ARP MAC lookup
│   ├── AlertService.cs            Unknown device + spike detection
│   └── PacketCaptureService.cs    SharpPcap live capture
├── Views/
│   ├── Home/Index.cshtml          Main dashboard
│   ├── Home/Logs.cshtml           Log viewer
│   └── Shared/_Layout.cshtml      Sidebar layout
├── wwwroot/
│   ├── css/site.css               Dark terminal theme
│   └── js/site.js                 Scan + toast logic
├── appsettings.json               SQL Server connection string
├── appsettings.Development.json   Dev overrides
└── NetSentinel.csproj
```

---

## 🧩 NuGet Packages

```xml
Microsoft.EntityFrameworkCore         8.0.0
Microsoft.EntityFrameworkCore.SqlServer  8.0.0   ← SQL Server 2019
Microsoft.EntityFrameworkCore.Tools   8.0.0
Microsoft.EntityFrameworkCore.Design  8.0.0
SharpPcap                             6.2.5
PacketDotNet                          1.4.7
```

---

## 🗺️ Roadmap

- [ ] Scheduled auto-scan (BackgroundService)
- [ ] Port scanner per device
- [ ] Email/Teams/Slack alert webhook
- [ ] Historical traffic charts (Chart.js)
- [ ] OS fingerprinting via TTL
- [ ] Docker + SQL Server 2019 container compose
- [ ] ASP.NET Identity login

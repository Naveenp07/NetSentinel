# NetSentinel 🛡️

A real-time network monitoring dashboard built with C# / ASP.NET Core 8.

## Features

- **Network Scanner** — Ping sweep across your subnet, resolves hostnames, reads ARP table for MAC addresses
- **Device Tracking** — SQLite database stores all discovered devices with first/last seen timestamps
- **Alerts System** — Detects unknown devices, traffic spikes, suspicious DNS queries
- **Packet Capture** — Live traffic capture via SharpPcap/Npcap (HTTP, DNS, TCP, UDP)
- **Real-time UI** — SignalR pushes alerts and device updates to the dashboard instantly
- **Live Logs** — Scrolling terminal-style log viewer

---

## Requirements

- [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8)
- **Windows:** [Npcap](https://npcap.com/) (for packet capture — install with "WinPcap API-compatible mode")
- **Linux/macOS:** `libpcap-dev` (`sudo apt install libpcap-dev`)

> Packet capture requires **Administrator** (Windows) or **sudo** (Linux/macOS).  
> Network scanning works without admin rights.

---

## Quick Start

```bash
# 1. Restore NuGet packages
dotnet restore

# 2. Run database migrations (auto-runs on startup via EnsureCreated)
#    No manual steps needed — SQLite DB is created automatically.

# 3. Run the app
dotnet run

# 4. Open in browser
#    http://localhost:5000
```

### Running with admin rights (for packet capture):

**Windows (PowerShell as Administrator):**
```powershell
dotnet run
```

**Linux/macOS:**
```bash
sudo dotnet run
```

---

## Configuration

Edit `appsettings.json` to set your subnet and scan parameters:

```json
{
  "NetSentinel": {
    "SubnetBase": "192.168.1",
    "ScanRangeStart": 1,
    "ScanRangeEnd": 254,
    "PingTimeoutMs": 500,
    "TrafficSpikeThresholdMB": 50.0
  }
}
```

---

## API Reference

| Method | Endpoint | Description |
|--------|----------|-------------|
| GET | `/api/devices` | List all devices |
| POST | `/api/devices/scan` | Trigger network scan |
| PUT | `/api/devices/{id}/trust` | Mark device as known/trusted |
| PUT | `/api/devices/{id}/label` | Set friendly label |
| GET | `/api/alerts` | List alerts |
| POST | `/api/alerts/{id}/acknowledge` | Acknowledge alert |
| POST | `/api/alerts/acknowledge-all` | Clear all alerts |
| GET | `/api/traffic/stats` | Live traffic stats |
| GET | `/api/traffic/logs` | Live packet log lines |
| GET | `/api/traffic/interfaces` | Available capture interfaces |
| POST | `/api/traffic/capture/start` | Start packet capture |
| POST | `/api/traffic/capture/stop` | Stop packet capture |

---

## Project Structure

```
NetSentinel/
├── Controllers/
│   ├── DevicesController.cs       ← Scan, list, trust devices
│   ├── AlertsController.cs        ← Alert management
│   ├── TrafficController.cs       ← Packet capture & stats
│   └── HomeController.cs          ← Dashboard & logs views
├── Models/
│   ├── Device.cs                  ← Device entity
│   ├── Alert.cs                   ← Alert entity + enums
│   ├── TrafficRecord.cs           ← Packet record + summary
│   └── ScanResult.cs              ← Scan history + log entry
├── Services/
│   ├── NetworkScannerService.cs   ← Ping sweep + ARP lookup
│   ├── AlertService.cs            ← Alert detection logic
│   └── PacketCaptureService.cs    ← SharpPcap live capture
├── Data/
│   └── AppDbContext.cs            ← EF Core + SQLite
├── Hubs/
│   └── NetworkHub.cs              ← SignalR real-time hub
├── Views/
│   ├── Home/
│   │   ├── Index.cshtml           ← Dashboard
│   │   └── Logs.cshtml            ← Log viewer
│   └── Shared/
│       └── _Layout.cshtml         ← Sidebar layout
└── wwwroot/
    ├── css/site.css               ← Dark terminal theme
    └── js/site.js                 ← Scan trigger, toasts, SignalR
```

---

## Build Roadmap

| Phase | Status | Description |
|-------|--------|-------------|
| 1 | ✅ Complete | Network scan, device list, SQLite storage |
| 2 | ✅ Complete | EF Core database, scan history |
| 3 | ✅ Complete | Alerts: unknown devices, traffic spikes |
| 4 | ✅ Complete | Packet capture via SharpPcap |

---

## Notes

- The SQLite database (`netsentinel.db`) is auto-created in the project root on first run.
- Newly discovered devices are marked `IsKnown = false` — use the `/api/devices/{id}/trust` endpoint or the dashboard to whitelist them.
- Packet capture requires Npcap on Windows. Download from [https://npcap.com](https://npcap.com) and install with **"WinPcap API-compatible mode"** checked.

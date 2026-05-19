// ===== NetSentinel site.js =====

// ---- Uptime counter ----
const startTime = Date.now();
function updateUptime() {
    const s = Math.floor((Date.now() - startTime) / 1000);
    const h = String(Math.floor(s / 3600)).padStart(2, '0');
    const m = String(Math.floor((s % 3600) / 60)).padStart(2, '0');
    const sec = String(s % 60).padStart(2, '0');
    const el = document.getElementById('uptimeVal');
    if (el) el.textContent = `${h}:${m}:${sec}`;
}
setInterval(updateUptime, 1000);

// ---- Live clock ----
function updateClock() {
    const el = document.getElementById('topbarTime');
    if (el) el.textContent = new Date().toLocaleTimeString('en-GB');
}
setInterval(updateClock, 1000);
updateClock();

// ---- Sidebar toggle (mobile) ----
function toggleSidebar() {
    const s = document.getElementById('sidebar');
    if (s) s.classList.toggle('open');
}

// ---- Scan with modal ----
async function triggerScan() {
    const btn = document.getElementById('scanBtn');
    const modal = document.getElementById('scanModal');
    const bar = document.getElementById('scanBar');
    const status = document.getElementById('scanStatus');
    if (!btn) return;

    btn.disabled = true;
    btn.textContent = '⟳ SCANNING...';
    if (modal) modal.style.display = 'flex';

    const stages = [
        [10, 'Sending ICMP ping sweep...'],
        [30, 'Collecting ARP responses...'],
        [55, 'Resolving hostnames...'],
        [75, 'Running alert checks...'],
        [90, 'Updating database...'],
        [98, 'Finalizing results...']
    ];

    let i = 0;
    const stageTimer = setInterval(() => {
        if (i < stages.length) {
            if (bar) bar.style.width = stages[i][0] + '%';
            if (status) status.textContent = stages[i][1];
            i++;
        }
    }, 600);

    try {
        const res = await fetch('/api/devices/scan', { method: 'POST' });
        const data = await res.json();
        clearInterval(stageTimer);
        if (bar) bar.style.width = '100%';
        if (status) status.textContent = 'Scan complete!';
        setTimeout(() => { if (modal) modal.style.display = 'none'; }, 700);
        showToast(`✔ Scan done — ${data.onlineCount} online, ${data.newDevicesFound} new`, 'ok');
        if (data.newAlerts > 0) showToast(`⚠ ${data.newAlerts} new alert(s) detected!`, 'warn');
        if (typeof loadDevices === 'function') loadDevices();
        if (typeof loadAlerts === 'function') loadAlerts();
        updateAlertBadge();
    } catch (e) {
        clearInterval(stageTimer);
        if (modal) modal.style.display = 'none';
        showToast('Scan failed: ' + e.message, 'danger');
    } finally {
        btn.disabled = false;
        btn.textContent = '▶ SCAN NETWORK';
        if (bar) bar.style.width = '0%';
    }
}

// ---- Toast ----
function showToast(msg, type = 'info') {
    const c = document.getElementById('toastContainer');
    if (!c) return;
    const t = document.createElement('div');
    t.className = `toast ${type}`;
    t.textContent = msg;
    c.appendChild(t);
    setTimeout(() => t.remove(), 4500);
}

// ---- Alert badge (topbar + nav) ----
async function updateAlertBadge() {
    try {
        const res = await fetch('/api/alerts/count');
        const data = await res.json();
        const count = data.unacknowledgedCount || 0;

        const navBadge = document.getElementById('alertNavBadge');
        if (navBadge) {
            navBadge.textContent = count;
            navBadge.style.display = count > 0 ? 'inline' : 'none';
        }
        const topBadge = document.getElementById('topbarAlerts');
        const topCount = document.getElementById('topbarAlertCount');
        if (topBadge) topBadge.style.display = count > 0 ? 'inline' : 'none';
        if (topCount) topCount.textContent = count;
    } catch {}
}

// ---- Device count badge ----
async function updateDeviceBadge() {
    try {
        const res = await fetch('/api/devices');
        const devs = await res.json();
        const online = devs.filter(d => d.isOnline).length;
        const nb = document.getElementById('navDeviceBadge');
        if (nb) nb.textContent = `${online} online`;
    } catch {}
}

// ---- Capture status ----
async function updateCaptureStatus() {
    try {
        const res = await fetch('/api/traffic/capture/status');
        const data = await res.json();
        const el = document.getElementById('captureStatus');
        if (el) {
            el.textContent = data.isCapturing ? 'ACTIVE' : 'IDLE';
            el.style.color = data.isCapturing ? 'var(--green)' : 'var(--muted)';
        }
    } catch {}
}

// ---- SignalR global ----
const connection = new signalR.HubConnectionBuilder()
    .withUrl('/networkHub')
    .withAutomaticReconnect()
    .build();

connection.on('ReceiveAlert', (alert) => {
    showToast('⚠ ' + alert.message, 'warn');
    updateAlertBadge();
    if (typeof loadAlerts === 'function') loadAlerts();
});
connection.on('ReceiveDeviceList', () => {
    if (typeof loadDevices === 'function') loadDevices();
    updateDeviceBadge();
});
connection.on('ReceiveLog', (msg) => {
    if (typeof addLiveLog === 'function') addLiveLog(msg);
});

connection.start().catch(e => console.warn('SignalR:', e));

// ---- Init ----
document.addEventListener('DOMContentLoaded', () => {
    updateAlertBadge();
    updateDeviceBadge();
    updateCaptureStatus();
    setInterval(updateAlertBadge, 15000);
    setInterval(updateDeviceBadge, 30000);
    setInterval(updateCaptureStatus, 10000);
});

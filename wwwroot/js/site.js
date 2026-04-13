// ===== NetSentinel — Global JS =====

// ---- Toast notifications ----
function showToast(message, type = 'info', duration = 4000) {
    const container = document.getElementById('toastContainer');
    if (!container) return;
    const toast = document.createElement('div');
    toast.className = `toast ${type}`;
    toast.textContent = message;
    container.appendChild(toast);
    setTimeout(() => {
        toast.style.opacity = '0';
        toast.style.transform = 'translateX(40px)';
        toast.style.transition = 'all 0.3s ease';
        setTimeout(() => toast.remove(), 300);
    }, duration);
}

// ---- Scan trigger ----
async function triggerScan() {
    const btn = document.getElementById('scanBtn');
    if (!btn) return;
    btn.disabled = true;
    btn.textContent = '⟳ SCANNING...';
    showToast('Network scan started...', 'info');

    try {
        const res = await fetch('/api/devices/scan', { method: 'POST' });
        if (!res.ok) throw new Error('Scan failed');
        const data = await res.json();
        showToast(`Scan complete — ${data.onlineCount} online, ${data.newDevicesFound} new device(s)`, 'ok');

        // Update last scan time
        const lastScan = document.getElementById('lastScan');
        if (lastScan) lastScan.textContent = new Date().toLocaleTimeString();

        // Reload dynamic content if functions exist
        if (typeof loadDevices === 'function') loadDevices();
        if (typeof loadAlerts === 'function') loadAlerts();
    } catch (e) {
        showToast('Scan error: ' + e.message, 'warn');
    } finally {
        btn.disabled = false;
        btn.textContent = '▶ SCAN NETWORK';
    }
}

// ---- Alert badge update ----
async function refreshAlertBadge() {
    try {
        const res = await fetch('/api/alerts/count');
        const data = await res.json();
        const badge = document.getElementById('alertBadge');
        if (!badge) return;
        if (data.unacknowledgedCount > 0) {
            badge.style.display = 'inline';
            badge.textContent = data.unacknowledgedCount;
        } else {
            badge.style.display = 'none';
        }
    } catch { }
}

// ---- Wave animation (sidebar-independent) ----
function initWave(containerId) {
    const container = document.getElementById(containerId);
    if (!container) return;
    for (let i = 0; i < 25; i++) {
        const col = document.createElement('div');
        col.className = 'wave-col';
        col.style.height = (Math.floor(Math.random() * 30) + 8) + 'px';
        container.appendChild(col);
    }
    setInterval(() => {
        const cols = container.querySelectorAll('.wave-col');
        const idx = Math.floor(Math.random() * cols.length);
        cols[idx].style.height = (Math.floor(Math.random() * 38) + 6) + 'px';
    }, 350);
}

// ---- Run on DOM ready ----
document.addEventListener('DOMContentLoaded', () => {
    refreshAlertBadge();
    setInterval(refreshAlertBadge, 15000);
});

// ===== NetSentinel — site.js =====

// ---- Scan trigger ----
async function triggerScan() {
    const btn = document.getElementById('scanBtn');
    if (!btn) return;
    btn.disabled = true;
    btn.textContent = '⟳ SCANNING...';
    showToast('Network scan started...', 'info');
    try {
        const res = await fetch('/api/devices/scan', { method: 'POST' });
        const data = await res.json();
        showToast(`✔ Scan complete — ${data.onlineCount} devices online, ${data.newDevicesFound} new`, 'ok');
        if (data.newAlerts > 0) showToast(`⚠ ${data.newAlerts} new alert(s) detected!`, 'warn');
        if (typeof loadDevices === 'function') loadDevices();
        if (typeof loadAlerts === 'function') loadAlerts();
    } catch (e) {
        showToast('Scan failed: ' + e.message, 'danger');
    } finally {
        btn.disabled = false;
        btn.textContent = '▶ SCAN NETWORK';
    }
}

// ---- Toast ----
function showToast(msg, type = 'info') {
    const container = document.getElementById('toastContainer');
    if (!container) return;
    const toast = document.createElement('div');
    toast.className = `toast ${type}`;
    toast.textContent = msg;
    container.appendChild(toast);
    setTimeout(() => toast.remove(), 4000);
}

// ---- Alert badge global update ----
async function refreshAlertBadge() {
    try {
        const res = await fetch('/api/alerts/count');
        const data = await res.json();
        const badge = document.getElementById('alertBadge');
        if (badge) {
            badge.textContent = data.unacknowledgedCount;
            badge.style.display = data.unacknowledgedCount > 0 ? 'inline' : 'none';
        }
    } catch {}
}

// ---- Init ----
document.addEventListener('DOMContentLoaded', () => {
    refreshAlertBadge();
    setInterval(refreshAlertBadge, 15000);
});

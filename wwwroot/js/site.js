/* ══════════════════════════════════════════════════════════════
   JAA – Global JavaScript
══════════════════════════════════════════════════════════════ */

// ── Sidebar toggle ────────────────────────────────────────────
function toggleSidebar() {
    document.getElementById('sidebar')?.classList.toggle('open');
    document.getElementById('sidebarOverlay')?.classList.toggle('open');
}

// ── Navbar scroll effect ──────────────────────────────────────
(function () {
    const navbar = document.querySelector('.navbar-jaa');
    if (!navbar) return;
    window.addEventListener('scroll', () => navbar.classList.toggle('scrolled', window.scrollY > 60));
})();

// ── Scroll-to-top ─────────────────────────────────────────────
(function () {
    const btn = document.getElementById('scrollTop');
    if (!btn) return;
    window.addEventListener('scroll', () => btn.classList.toggle('visible', window.scrollY > 400));
    btn.addEventListener('click', () => window.scrollTo({ top: 0, behavior: 'smooth' }));
})();

// ── Hamburger menu ────────────────────────────────────────────
(function () {
    const toggle = document.getElementById('hamburger');
    const menu   = document.getElementById('mobileMenu');
    if (!toggle || !menu) return;
    toggle.addEventListener('click', () => menu.classList.toggle('open'));
})();

// ── Modal helpers ─────────────────────────────────────────────
function openModal(id) {
    const m = document.getElementById(id);
    if (!m) return;
    m.classList.add('open');
    document.body.style.overflow = 'hidden';
}
function closeModal(id) {
    const m = document.getElementById(id);
    if (!m) return;
    m.classList.remove('open');
    document.body.style.overflow = '';
}
document.addEventListener('click', e => {
    if (e.target.classList.contains('modal-backdrop')) {
        e.target.classList.remove('open');
        document.body.style.overflow = '';
    }
});
document.addEventListener('keydown', e => {
    if (e.key === 'Escape')
        document.querySelectorAll('.modal-backdrop.open').forEach(m => {
            m.classList.remove('open');
            document.body.style.overflow = '';
        });
});

// ── Symptom chips ─────────────────────────────────────────────
function toggleChip(el) {
    el.classList.toggle('selected');
    const field = document.getElementById('selectedSymptoms');
    if (field) {
        const selected = [...document.querySelectorAll('.chip.selected')]
            .map(c => c.dataset.value || c.textContent.trim());
        field.value = selected.join(', ');
    }
}

// ── Star rating ───────────────────────────────────────────────
function setRating(value) {
    document.querySelectorAll('.star-input').forEach((s, i) =>
        s.classList.toggle('selected', i < value));
    const field = document.getElementById('ratingValue');
    if (field) field.value = value;
}

// ── Photo preview ─────────────────────────────────────────────
function previewPhotos(input) {
    const preview = document.getElementById('photoPreview');
    if (!preview) return;
    preview.innerHTML = '';
    Array.from(input.files).slice(0, 5).forEach(file => {
        const reader = new FileReader();
        reader.onload = e => {
            const wrap = document.createElement('div');
            wrap.style.cssText = 'position:relative;width:78px;height:78px;border-radius:8px;overflow:hidden;border:1px solid var(--border);flex-shrink:0;';
            wrap.innerHTML = `<img src="${e.target.result}" style="width:100%;height:100%;object-fit:cover;" alt="preview" />`;
            preview.appendChild(wrap);
        };
        reader.readAsDataURL(file);
    });
}

// ── Availability toggle ───────────────────────────────────────
function toggleAvailability(cb) {
    const label = document.getElementById('availLabel');
    if (!label) return;
    label.textContent = cb.checked ? 'Online' : 'Offline';
    label.style.color = cb.checked ? 'var(--green-t)' : 'var(--muted)';
}

// ── Guest auth modal ──────────────────────────────────────────
function handleGuestSubmit() {
    const isAuth = document.getElementById('isAuthenticated')?.value === 'true';
    if (!isAuth) { openModal('quickAuthModal'); return false; }
    return true;
}
function confirmGuestSubmit() {
    const name  = document.getElementById('guestName')?.value.trim();
    const phone = document.getElementById('guestPhone')?.value.trim();
    const err   = document.getElementById('quickAuthError');
    if (!name || !phone) { if (err) err.style.display = 'flex'; return; }
    if (err) err.style.display = 'none';

    const form = document.getElementById('requestForm');
    ['GuestFullName','GuestPhone'].forEach((n,i) => {
        let f = form.querySelector(`[name="${n}"]`);
        if (!f) { f = document.createElement('input'); f.type='hidden'; f.name=n; form.appendChild(f); }
        f.value = [name, phone][i];
    });
    closeModal('quickAuthModal');
    form.submit();
}

// ── Table search / filter ─────────────────────────────────────
function filterTableByText(inputEl, tableId) {
    const q = inputEl.value.toLowerCase();
    document.querySelectorAll(`#${tableId} tbody tr`).forEach(tr =>
        tr.style.display = tr.textContent.toLowerCase().includes(q) ? '' : 'none');
}

// ── Scroll-based fade-in ──────────────────────────────────────
(function () {
    const els = document.querySelectorAll('.fade-in');
    if (!els.length) return;
    const obs = new IntersectionObserver(entries => {
        entries.forEach(entry => {
            if (!entry.isIntersecting) return;
            setTimeout(() => {
                entry.target.style.opacity = '1';
                entry.target.style.transform = 'translateY(0)';
            }, (entry.target.dataset.delay || 0) * 80);
            obs.unobserve(entry.target);
        });
    }, { threshold: 0.1 });

    els.forEach((el, i) => {
        el.style.opacity = '0';
        el.style.transform = 'translateY(18px)';
        el.style.transition = 'opacity .5s ease, transform .5s ease';
        el.dataset.delay = i % 4;
        obs.observe(el);
    });
})();

/* ══════════════════════════════════════════════════════════════
   MAP HELPERS  (Leaflet.js — loaded on-demand per page)
   OpenStreetMap tiles — no API key required.
══════════════════════════════════════════════════════════════ */

const JAA_MAP_TILE = 'https://{s}.tile.openstreetmap.org/{z}/{x}/{y}.png';
const JAA_MAP_ATTR = '© <a href="https://openstreetmap.org">OpenStreetMap</a> contributors';

/**
 * initCustomerMap(lat, lng, elementId)
 * Shows customer location pin on a Leaflet map.
 */
function initCustomerMap(lat, lng, elementId = 'locationMap') {
    if (typeof L === 'undefined') return;
    const el = document.getElementById(elementId);
    if (!el) return;

    const map = L.map(elementId, { zoomControl: true, scrollWheelZoom: false }).setView([lat, lng], 15);
    L.tileLayer(JAA_MAP_TILE, { attribution: JAA_MAP_ATTR }).addTo(map);

    const icon = L.divIcon({
        html: `<div style="width:36px;height:36px;background:var(--red,#e63946);border:3px solid #fff;border-radius:50% 50% 50% 0;transform:rotate(-45deg);box-shadow:0 2px 12px rgba(230,57,70,.5);"></div>`,
        iconSize: [36, 36], iconAnchor: [18, 36], className: ''
    });

    L.marker([lat, lng], { icon })
        .addTo(map)
        .bindPopup('<b>Your Location</b>')
        .openPopup();

    return map;
}

/**
 * initShopsMap(shops, elementId, customerLat, customerLng)
 * Shows all verified shops as pins, optionally centred on customer.
 * shops = [{ id, shopName, city, address, latitude, longitude, phoneNumber }]
 */
function initShopsMap(shops, elementId = 'shopsMap', customerLat = null, customerLng = null) {
    if (typeof L === 'undefined') return;
    const el = document.getElementById(elementId);
    if (!el) return;

    // Default centre: Amman
    const centre = (customerLat && customerLng)
        ? [customerLat, customerLng]
        : [31.9539, 35.9106];

    const map = L.map(elementId, { scrollWheelZoom: false }).setView(centre, 12);
    L.tileLayer(JAA_MAP_TILE, { attribution: JAA_MAP_ATTR }).addTo(map);

    // Customer pin
    if (customerLat && customerLng) {
        const youIcon = L.divIcon({
            html: `<div style="width:18px;height:18px;background:#58a6ff;border:3px solid #fff;border-radius:50%;box-shadow:0 2px 8px rgba(88,166,255,.6);"></div>`,
            iconSize: [18, 18], iconAnchor: [9, 9], className: ''
        });
        L.marker([customerLat, customerLng], { icon: youIcon })
            .addTo(map).bindPopup('<b>Your Location</b>');
    }

    // Shop pins
    const shopIcon = L.divIcon({
        html: `<div style="width:32px;height:32px;background:#e63946;border:3px solid #fff;border-radius:8px;display:flex;align-items:center;justify-content:center;font-size:14px;box-shadow:0 2px 10px rgba(230,57,70,.5);">🔧</div>`,
        iconSize: [32, 32], iconAnchor: [16, 32], className: ''
    });

    shops.forEach(s => {
        if (!s.latitude || !s.longitude) return;
        L.marker([s.latitude, s.longitude], { icon: shopIcon })
            .addTo(map)
            .bindPopup(`
                <div style="min-width:180px;">
                  <b style="color:#e63946;">${s.shopName}</b><br>
                  <small>${s.city}${s.address ? ' · ' + s.address : ''}</small><br>
                  ${s.phoneNumber ? `<small>📞 ${s.phoneNumber}</small>` : ''}
                </div>
            `);
    });

    return map;
}

/**
 * Auto-detect GPS and show on map for the request page.
 */
function autoDetectAndMap(mapElementId = 'locationMap') {
    const btn    = document.getElementById('shareLocationBtn');
    const status = document.getElementById('locationStatus');
    const txt    = document.getElementById('locationText');

    if (!navigator.geolocation) {
        alert('Geolocation is not supported by your browser. Please enter your location manually.');
        return;
    }

    if (btn) { btn.disabled = true; btn.innerHTML = '<i class="bi bi-arrow-repeat"></i> Locating…'; }

    navigator.geolocation.getCurrentPosition(
        pos => {
            const { latitude: lat, longitude: lng } = pos.coords;

            // Fill hidden fields
            const latF = document.getElementById('Latitude');
            const lngF = document.getElementById('Longitude');
            if (latF) latF.value = lat;
            if (lngF) lngF.value = lng;

            // Reverse geocode with Nominatim (free)
            fetch(`https://nominatim.openstreetmap.org/reverse?format=json&lat=${lat}&lon=${lng}&zoom=16`)
                .then(r => r.json())
                .then(data => {
                    const addr = data.display_name || `${lat.toFixed(5)}, ${lng.toFixed(5)}`;
                    const locF = document.getElementById('CustomerLocation');
                    if (locF && !locF.value) locF.value = addr;
                    if (txt) txt.textContent = addr;
                })
                .catch(() => { if (txt) txt.textContent = `${lat.toFixed(5)}° N, ${lng.toFixed(5)}° E`; });

            if (status) status.style.display = 'flex';
            if (btn) { btn.disabled = false; btn.innerHTML = '<i class="bi bi-check-circle-fill"></i> Location Detected'; btn.classList.add('btn-green'); btn.classList.remove('btn-outline'); }

            // Show map
            const mapEl = document.getElementById(mapElementId);
            if (mapEl) {
                mapEl.style.height = '240px';
                mapEl.style.borderRadius = '10px';
                mapEl.style.overflow = 'hidden';
                initCustomerMap(lat, lng, mapElementId);
            }
        },
        err => {
            if (btn) { btn.disabled = false; btn.innerHTML = '<i class="bi bi-crosshair"></i> Use My GPS Location'; }
            const msgs = {
                1: 'Location access denied. Please allow location in your browser, or enter it manually below.',
                2: 'Could not detect your position. Please enter your location manually.',
                3: 'Location request timed out. Please try again or enter manually.'
            };
            alert(msgs[err.code] || 'Could not get location.');
        },
        { timeout: 10000, enableHighAccuracy: true }
    );
}

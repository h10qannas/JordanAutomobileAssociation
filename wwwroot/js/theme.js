// JAA Theme Manager — dark/light mode with localStorage persistence
(function () {
  const STORAGE_KEY = 'jaa-theme';
  const DARK  = 'dark';
  const LIGHT = 'light';

  function getSavedTheme() {
    try { return localStorage.getItem(STORAGE_KEY) || DARK; }
    catch { return DARK; }
  }

  function applyTheme(theme) {
    document.documentElement.setAttribute('data-theme', theme);
    const icon = document.getElementById('themeIcon');
    if (icon) {
      icon.className = theme === DARK ? 'bi bi-sun-fill' : 'bi bi-moon-fill';
    }
    try { localStorage.setItem(STORAGE_KEY, theme); } catch {}
  }

  // Apply theme immediately (anti-flash — also covered by inline script in <head>)
  applyTheme(getSavedTheme());

  window.toggleTheme = function () {
    const current = getSavedTheme();
    applyTheme(current === DARK ? LIGHT : DARK);
  };

  // After DOM ready: sync icon and ensure correct dir attribute
  document.addEventListener('DOMContentLoaded', function () {
    // Sync theme icon (may have loaded before DOM was ready)
    applyTheme(getSavedTheme());

    // Enforce dir attribute from server-set lang (server already sets it in HTML,
    // this ensures it is correct if JS ever runs before the attribute is set)
    const lang = document.documentElement.getAttribute('lang') || 'ar';
    document.documentElement.setAttribute('dir', lang.startsWith('ar') ? 'rtl' : 'ltr');
  });
})();

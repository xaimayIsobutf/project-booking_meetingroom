(function () {
  const names = {
    en: { profile: 'Profile', settings: 'Organization settings' },
    th: { profile: 'โปรไฟล์', settings: 'การตั้งค่าองค์กร' },
    lo: { profile: 'ໂປຣໄຟລ໌', settings: 'ການຕັ້ງຄ່າອົງກອນ' }
  };
  function apply() {
    const code = localStorage.getItem('roombooking-language') || 'th';
    const text = names[code] || names.th;
    document.querySelectorAll('[data-profile-i18n="profile"]').forEach(e => e.textContent = text.profile);
    document.querySelectorAll('[data-profile-i18n="organizationSettings"]').forEach(e => e.textContent = text.settings);
    const settingsNav = document.querySelector('[data-nav="settings"] em');
    if (settingsNav) settingsNav.textContent = text.settings;
    if (location.pathname === '/profile') {
      const heading = document.querySelector('.topbar-left h2');
      if (heading) heading.textContent = text.profile;
      document.title = text.profile + ' - RoomBooking';
    }
  }
  apply();
  document.getElementById('languageSelect')?.addEventListener('change', () => setTimeout(apply, 120));
})();

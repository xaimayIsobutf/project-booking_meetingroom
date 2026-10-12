(() => {
    const dialog = document.getElementById('pairDisplayDialog');
    if (!dialog) return;
    const nameInput = document.getElementById('pairDisplayName');
    const roomSelect = document.getElementById('pairDisplayRoom');
    const result = document.getElementById('pairDisplayResult');
    const identity = document.createElement('div');
    identity.className = 'pair-display-identity';
    identity.innerHTML = '<strong class="pair-display-result-name"></strong><span class="pair-display-result-room"></span>';
    result?.prepend(identity);
    const syncIdentity = () => {
        const name = identity.querySelector('.pair-display-result-name');
        const room = identity.querySelector('.pair-display-result-room');
        if (name) name.textContent = nameInput?.value?.trim() || 'Room display';
        if (room) room.textContent = roomSelect?.selectedOptions?.[0]?.textContent?.trim() || '';
    };
    document.addEventListener('click', event => {
        const button = event.target.closest?.('.display-regenerate');
        if (!button) return;
        const row = button.closest('.paired-display-row, .room-display-device-row');
        const displayName = row?.querySelector('.paired-display-main strong, div strong')?.textContent?.trim();
        const roomName = row?.querySelector('.paired-display-main span, div span')?.textContent?.trim();
        if (displayName && nameInput) nameInput.value = displayName;
        if (roomName && roomSelect) [...roomSelect.options].forEach(option => { if (option.textContent.includes(roomName)) roomSelect.value = option.value; });
        syncIdentity();
    }, true);
    document.addEventListener('click', async event => {
        const button = event.target.closest?.('.display-revoke');
        if (!button) return;
        event.preventDefault();
        event.stopImmediatePropagation();
        const language = localStorage.getItem('roombooking-language') || 'en';
        const message = language === 'th'
            ? 'ยืนยันยกเลิกการจับคู่อุปกรณ์นี้หรือไม่?'
            : language === 'lo'
                ? 'ຢືນຢັນຍົກເລີກການຈັບຄູ່ອຸປະກອນນີ້ບໍ?'
                : 'Unpair this display?';
        if (!window.confirm(message)) return;
        button.disabled = true;
        const response = await fetch(`/api/v1/room-display/devices/${button.dataset.deviceId}/revoke`, { method: 'POST' });
        if (response.ok) {
            button.closest('.paired-display-row, .room-display-device-row')?.remove();
        } else {
            button.disabled = false;
        }
    }, true);
    // Watch only the result visibility. Observing childList here causes an
    // endless loop because syncIdentity() updates textContent inside result.
    new MutationObserver(syncIdentity).observe(result || dialog, { attributes: true, attributeFilter: ['hidden'] });
    roomSelect?.addEventListener('change', syncIdentity);
    nameInput?.addEventListener('input', syncIdentity);
    syncIdentity();

    // Keep long paired-display lists compact. The existing page script owns
    // fetching/filtering the data; this layer only paginates rendered rows.
    const list = document.getElementById('displayDeviceList');
    const panel = list?.closest('.room-display-devices-panel');
    if (!list || !panel) return;
    const pageSize = 5;
    let page = 1;
    const pager = document.createElement('div');
    pager.className = 'paired-display-pagination';
    panel.append(pager);

    const applyPagination = (reset = false) => {
        const rows = [...list.querySelectorAll('.paired-display-row, .room-display-device-row')];
        if (reset) page = 1;
        const pages = Math.max(1, Math.ceil(rows.length / pageSize));
        page = Math.min(page, pages);
        rows.forEach((row, index) => {
            row.hidden = index < (page - 1) * pageSize || index >= page * pageSize;
        });
        pager.hidden = rows.length <= pageSize;
        pager.innerHTML = rows.length <= pageSize ? '' : `
            <span class="paired-display-pagination-info">${(page - 1) * pageSize + 1}–${Math.min(page * pageSize, rows.length)} / ${rows.length}</span>
            <button type="button" class="button ghost" data-page="prev" ${page === 1 ? 'disabled' : ''}>‹</button>
            <span class="paired-display-pagination-page">${page} / ${pages}</span>
            <button type="button" class="button ghost" data-page="next" ${page === pages ? 'disabled' : ''}>›</button>`;
        pager.querySelector('[data-page="prev"]')?.addEventListener('click', () => { page--; applyPagination(); });
        pager.querySelector('[data-page="next"]')?.addEventListener('click', () => { page++; applyPagination(); });
    };

    new MutationObserver(() => applyPagination(true)).observe(list, { childList: true });
    applyPagination();
})();

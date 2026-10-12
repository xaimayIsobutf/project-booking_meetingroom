(() => {
    const queueKey = 'roombooking-toast';
    const messages = {
        en: { saved: 'Saved successfully', created: 'Created successfully', deleted: 'Deleted successfully', cancelled: 'Cancelled successfully' },
        th: { saved: 'บันทึกสำเร็จ', created: 'สร้างสำเร็จ', deleted: 'ลบสำเร็จ', cancelled: 'ยกเลิกสำเร็จ' },
        lo: { saved: 'ບັນທຶກສຳເລັດ', created: 'ສ້າງສຳເລັດ', deleted: 'ລຶບສຳເລັດ', cancelled: 'ຍົກເລີກສຳເລັດ' }
    };
    const language = () => localStorage.getItem('roombooking-language') || 'th';
    const messageFor = (url) => {
        const path = String(url).toLowerCase();
        const kind = path.includes('delete') || path.includes('deactivate') || path.includes('remove') || path.includes('revoke')
            ? 'deleted' : path.includes('cancel') || path.includes('unpair') ? 'cancelled'
            : path.includes('create') || path.includes('register') || path.includes('pair/start') ? 'created' : 'saved';
        return (messages[language()] || messages.th)[kind];
    };
    const show = (message, type = 'success') => {
        let toast = document.getElementById('globalToast');
        if (!toast) return;
        toast.textContent = message;
        toast.className = `global-toast ${type}`;
        toast.hidden = false;
        clearTimeout(show.timer);
        show.timer = setTimeout(() => { toast.hidden = true; }, 3500);
    };
    const queue = message => {
        try {
            sessionStorage.setItem(queueKey, message);
            setTimeout(() => sessionStorage.removeItem(queueKey), 2500);
        } catch (_) { }
    };
    const pending = sessionStorage.getItem(queueKey);
    if (pending) { sessionStorage.removeItem(queueKey); window.addEventListener('DOMContentLoaded', () => show(pending)); }
    window.addEventListener('DOMContentLoaded', () => {
        const flash = document.getElementById('globalToast')?.dataset.message;
        if (flash) show(flash);
    });
    const nativeFetch = window.fetch.bind(window);
    window.fetch = async (input, init = {}) => {
        const request = input instanceof Request ? input : null;
        const method = String(init.method || request?.method || 'GET').toUpperCase();
        const url = input instanceof Request ? input.url : String(input);
        const headers = new Headers(init.headers || request?.headers || {});
        const response = await nativeFetch(input, init);
        if (['POST', 'PUT', 'PATCH', 'DELETE'].includes(method) && response.ok && headers.get('X-Silent-Notification') !== '1') {
            const message = messageFor(url);
            show(message);
            queue(message);
        }
        return response;
    };
    window.roomBookingToast = show;
})();

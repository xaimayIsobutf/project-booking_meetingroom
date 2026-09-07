// Please see documentation at https://learn.microsoft.com/aspnet/core/client-side/bundling-and-minification
// for details on configuring this project to bundle and minify static web assets.

// Write your JavaScript code.
document.addEventListener('DOMContentLoaded', () => {
    const language = document.querySelector('.language-select');
    if (!language) return;
    const saved = localStorage.getItem('roombooking-language') || 'en';
    language.value = saved;
    const labels = {
        en: { workspace: 'WORKSPACE', team: 'TEAM', dashboard: 'Dashboard', rooms: 'Meeting rooms', calendar: 'Calendar Room', reports: 'Reports', reservations: 'Reservations', approvals: 'Approvals', members: 'Members', logout: 'Sign out' },
        th: { workspace: 'พื้นที่ทำงาน', team: 'ทีม', dashboard: 'แดชบอร์ด', rooms: 'ห้องประชุม', calendar: 'ปฏิทินห้อง', reports: 'รายงาน', reservations: 'รายการจอง', approvals: 'อนุมัติ', members: 'สมาชิก', logout: 'ออกจากระบบ' },
        lo: { workspace: 'ພື້ນທີ່ເຮັດວຽກ', team: 'ທີມງານ', dashboard: 'ແດຊບອດ', rooms: 'ຫ້ອງປະຊຸມ', calendar: 'ປະຕິທິນຫ້ອງ', reports: 'ລາຍງານ', reservations: 'ການຈອງ', approvals: 'ອະນຸມັດ', members: 'ສະມາຊິກ', logout: 'ອອກຈາກລະບົບ' }
    };
    const uiText = {
        'ห้องประชุม': ['Meeting rooms','ห้องประชุม','ຫ້ອງປະຊຸມ'], 'รายงาน': ['Reports','รายงาน','ລາຍງານ'], 'รายการจองห้อง': ['Reservations','รายการจองห้อง','ການຈອງຫ້ອງ'], 'ปฏิทินห้อง': ['Room calendar','ปฏิทินห้อง','ປະຕິທິນຫ້ອງ'], 'สมาชิก': ['Members','สมาชิก','ສະມາຊິກ'], 'เพิ่มห้องประชุม': ['Add meeting room','เพิ่มห้องประชุม','ເພີ່ມຫ້ອງປະຊຸມ'], 'แก้ไขห้องประชุม': ['Edit meeting room','แก้ไขห้องประชุม','ແກ້ໄຂຫ້ອງປະຊຸມ'], 'บันทึก': ['Save','บันทึก','ບັນທຶກ'], 'บันทึกการแก้ไข': ['Save changes','บันทึกการแก้ไข','ບັນທຶກການແກ້ໄຂ'], 'ยกเลิก': ['Cancel','ยกเลิก','ຍົກເລີກ'], 'จองห้องประชุม': ['Book a room','จองห้องประชุม','ຈອງຫ້ອງປະຊຸມ'], 'จองห้อง': ['Book room','จองห้อง','ຈອງຫ້ອງ'], 'การจองวันนี้': ["Today's bookings",'การจองวันนี้','ການຈອງມື້ນີ້'], 'ห้องที่ว่างตอนนี้': ['Available rooms','ห้องที่ว่างตอนนี้','ຫ້ອງວ່າງຕອນນີ້'], 'ห้องประชุมทั้งหมด': ['Total rooms','ห้องประชุมทั้งหมด','ຫ້ອງປະຊຸມທັງໝົດ'], 'สถานะ': ['Status','สถานะ','ສະຖານະ'], 'การจัดการ': ['Actions','การจัดการ','ການຈັດການ'], 'ค้นหา': ['Search','ค้นหา','ຄົ້ນຫາ'], 'กรอง': ['Filter','กรอง','ກັ່ນຕອງ'], 'เปิดใช้งาน': ['Active','เปิดใช้งาน','ເປີດໃຊ້ງານ'], 'ปิดใช้งาน': ['Inactive','ปิดใช้งาน','ປິດໃຊ້ງານ'], 'อนุมัติ': ['Approvals','อนุมัติ','ອະນຸມັດ'], 'ภาพรวมการจองห้องประชุมของบริษัท':['Company booking overview','ภาพรวมการจองห้องประชุมของบริษัท','ພາບລວມການຈອງຫ້ອງປະຊຸມ'], 'เพิ่มการจอง':['Add booking','เพิ่มการจอง','ເພີ່ມການຈອງ'], 'จัดการ':['Manage','จัดการ','ຈັດການ'], 'ห้องว่าง':['Available','ห้องว่าง','ຫ້ອງວ່າງ'], 'กำลังใช้งาน':['In use','กำลังใช้งาน','ກຳລັງໃຊ້ງານ'], 'จองแล้ว':['Booked','จองแล้ว','ຈອງແລ້ວ'], 'ห้องที่ถูกจองบ่อย':['Most booked rooms','ห้องที่ถูกจองบ่อย','ຫ້ອງທີ່ຖືກຈອງຫຼາຍ'], 'รายละเอียดการใช้ห้องประชุม':['Meeting room usage details','รายละเอียดการใช้ห้องประชุม','ລາຍລະອຽດການໃຊ້ຫ້ອງປະຊຸມ'], 'แชร์ห้อง':['Share room','แชร์ห้อง','ແຊຫ້ອງ'], 'ลบห้อง':['Delete room','ลบห้อง','ລຶບຫ້ອງ']
    };
    const keyedText = {
        'dashboard.title': ['Company booking overview', 'ภาพรวมการจองห้องประชุมของบริษัท', 'ພາບລວມການຈອງຫ້ອງປະຊຸມ'],
        'dashboard.total': ['Total rooms', 'ห้องประชุมทั้งหมด', 'ຫ້ອງປະຊຸມທັງໝົດ'],
        'dashboard.today': ["Today's bookings", 'การจองวันนี้', 'ການຈອງມື້ນີ້'],
        'dashboard.available': ['Available rooms', 'ห้องที่ว่างตอนนี้', 'ຫ້ອງວ່າງຕອນນີ້'],
        'common.book': ['+ Book a room', '+ จองห้องประชุม', '+ ຈອງຫ້ອງປະຊຸມ'],
        'nav.rooms': ['Meeting rooms', 'ห้องประชุม', 'ຫ້ອງປະຊຸມ'],
        'reservations.title': ['Reservations', 'รายการจองห้อง', 'ການຈອງຫ້ອງ'],
        'calendar.title': ['Calendar Room', 'ปฏิทินห้อง', 'ປະຕິທິນຫ້ອງ'],
        'rooms.directory': ['Meeting room directory', 'ไดเรกทอรีห้องประชุม', 'ໄດເຣັກທໍຣີຫ້ອງປະຊຸມ'],
        'rooms.description': ['Company meeting room information with search and status filters', 'ข้อมูลห้องประชุมของบริษัท พร้อมค้นหาและกรองตามสถานะ', 'ຂໍ້ມູນຫ້ອງປະຊຸມຂອງບໍລິສັດ ພ້ອມຄົ້ນຫາ ແລະກັ່ນຕອງຕາມສະຖານະ']
        , 'reports.title': ['Meeting room usage reports', 'รายงานการใช้ห้องประชุม', 'ລາຍງານການໃຊ້ຫ້ອງປະຊຸມ'], 'reports.description': ['Company meeting room booking summary', 'สรุปข้อมูลการจองห้องประชุมของบริษัท', 'ສະຫຼຸບຂໍ້ມູນການຈອງຫ້ອງປະຊຸມຂອງບໍລິສັດ'], 'reports.exportExcel': ['Export Excel','ส่งออก Excel','ສົ່ງອອກ Excel'], 'reports.exportPdf': ['Export PDF','ส่งออก PDF','ສົ່ງອອກ PDF'], 'reports.from': ['From','ตั้งแต่','ຕັ້ງແຕ່'], 'reports.to': ['To','ถึง','ເຖິງ'], 'reports.total': ['Total bookings','การจองทั้งหมด','ການຈອງທັງໝົດ'], 'reports.hours': ['Usage hours','ชั่วโมงการใช้งาน','ຊົ່ວໂມງການໃຊ້ງານ'], 'reports.shared': ['Bookings from shared rooms','การจองจากห้องแชร์','ການຈອງຈາກຫ້ອງທີ່ແຊຣ໌'], 'reports.frequent': ['Most booked rooms','ห้องที่ถูกจองบ่อย','ຫ້ອງທີ່ຖືກຈອງຫຼາຍ'], 'reports.byStatus': ['Summary by status','สรุปตามสถานะ','ສະຫຼຸບຕາມສະຖານະ'], 'reports.monthly': ['Monthly bookings','การจองรายเดือน','ການຈອງລາຍເດືອນ'], 'reports.users': ['Top organizers','ผู้จองที่ใช้ห้องมากที่สุด','ຜູ້ຈອງທີ່ໃຊ້ຫ້ອງຫຼາຍທີ່ສຸດ'], 'reports.detail': ['Meeting room usage details','รายละเอียดการใช้ห้องประชุม','ລາຍລະອຽດການໃຊ້ຫ້ອງປະຊຸມ'], 'common.status':['Status','สถานะ','ສະຖານະ'], 'common.filter':['Filter','กรอง','ກັ່ນຕອງ'], 'common.items':['items','รายการ','ລາຍການ'], 'common.hours':['hours','ชั่วโมง','ຊົ່ວໂມງ'], 'common.page':['Page','หน้า','ໜ້າ'], 'common.of':['of','จาก','ຈາກ']
    };
    Object.assign(uiText, { 'บริษัท':['Company','บริษัท','ບໍລິສັດ'], 'อีเมล':['Email','อีเมล','ອີເມວ'], 'รหัสผ่าน':['Password','รหัสผ่าน','ລະຫັດຜ່ານ'], 'หัวข้อการประชุม':['Meeting title','หัวข้อการประชุม','ຫົວຂໍ້ການປະຊຸມ'], 'ผู้จอง':['Organizer','ผู้จอง','ຜູ້ຈອງ'], 'วันเวลา':['Date & time','วันเวลา','ວັນທີ ແລະ ເວລາ'], 'เพิ่มสมาชิก':['Add member','เพิ่มสมาชิก','ເພີ່ມສະມາຊິກ'], 'แก้ไข':['Edit','แก้ไข','ແກ້ໄຂ'], 'ปิดใช้งาน':['Deactivate','ปิดใช้งาน','ປິດໃຊ້ງານ'], 'ปฏิเสธ':['Reject','ปฏิเสธ','ປະຕິເສດ'], 'อุปกรณ์':['Facilities','อุปกรณ์','ອຸປະກອນ'], 'ตึก':['Building','ตึก','ອາຄານ'], 'ชั้น':['Floor','ชั้น','ຊັ້ນ'] });
    Object.assign(uiText, {
        'Meeting room directory': ['Meeting room directory', 'ไดเรกทอรีห้องประชุม', 'ໄດເຣັກທໍຣີຫ້ອງປະຊຸມ'],
        'ข้อมูลห้องประชุมของบริษัท พร้อมค้นหาและกรองตามสถานะ': ['Company meeting room information with search and status filters', 'ข้อมูลห้องประชุมของบริษัท พร้อมค้นหาและกรองตามสถานะ', 'ຂໍ້ມູນຫ້ອງປະຊຸມຂອງບໍລິສັດ ພ້ອມຄົ້ນຫາ ແລະກັ່ນຕອງຕາມສະຖານະ'],
        'ขนาดห้อง': ['Room size', 'ขนาดห้อง', 'ຂະໜາດຫ້ອງ'],
        'รองรับ': ['Capacity', 'รองรับ', 'ຮອງຮັບ'],
        'การแชร์': ['Sharing', 'การแชร์', 'ການແຊຣ໌'],
        'Meeting room': ['Meeting room', 'ห้องประชุม', 'ຫ້ອງປະຊຸມ'],
        'ค้นหาชื่อห้อง ตึก ชั้น หรืออุปกรณ์...': ['Search room name, building, floor or facility...', 'ค้นหาชื่อห้อง ตึก ชั้น หรืออุปกรณ์...', 'ຄົ້ນຫາຊື່ຫ້ອງ, ອາຄານ, ຊັ້ນ ຫຼື ອຸປະກອນ...'],
        'ทุกสถานะ': ['All statuses', 'ทุกสถานะ', 'ທຸກສະຖານະ'],
        'เพิ่มห้องประชุม': ['Add meeting room', 'เพิ่มห้องประชุม', 'ເພີ່ມຫ້ອງປະຊຸມ'],
        'ตรวจสอบห้องว่างตามวันและช่วงเวลาที่ต้องการจอง': ['Check room availability by date and time', 'ตรวจสอบห้องว่างตามวันและช่วงเวลาที่ต้องการจอง', 'ກວດເບິ່ງຫ້ອງວ່າງຕາມວັນ ແລະເວລາທີ່ຕ້ອງການຈອງ'],
        'รายวัน': ['Day', 'รายวัน', 'ລາຍວັນ'],
        'รายสัปดาห์': ['Week', 'รายสัปดาห์', 'ລາຍອາທິດ'],
        'เลือกวันที่': ['Select date', 'เลือกวันที่', 'ເລືອກວັນທີ'],
        'ทุกห้องประชุม': ['All meeting rooms', 'ทุกห้องประชุม', 'ຫ້ອງປະຊຸມທັງໝົດ'],
        'ว่าง': ['Available', 'ว่าง', 'ຫ້ອງວ່າງ'],
        'จองแล้ว': ['Booked', 'จองแล้ว', 'ຈອງແລ້ວ'],
        'ห้องประชุม': ['Meeting rooms', 'ห้องประชุม', 'ຫ້ອງປະຊຸມ'],
        'สัปดาห์': ['Week', 'สัปดาห์', 'ອາທິດ']
    });
    const applyLanguage = code => {
        const t = labels[code] || labels.en;
        document.documentElement.lang = code === 'th' ? 'th' : code === 'lo' ? 'lo' : 'en';
        document.querySelectorAll('.sidebar-label').forEach((e, i) => e.textContent = i === 0 ? t.workspace : t.team);
        const keys = ['dashboard','rooms','calendar','reports','reservations','approvals','members'];
        document.querySelectorAll('.side-nav').forEach((nav, group) => nav.querySelectorAll('a').forEach((e, i) => { const span=e.querySelector('span'); const icon=span?.outerHTML || ''; const key=group === 0 ? keys[i] : 'members'; e.innerHTML=icon+' '+(t[key] || e.textContent.trim()); }));
        const logout = document.querySelector('.header-logout span'); if (logout) logout.textContent = t.logout;
        const index = code === 'th' ? 1 : code === 'lo' ? 2 : 0;
        document.querySelectorAll('[data-i18n]').forEach(e => { const values = keyedText[e.dataset.i18n]; if (values && !e.matches('.side-nav a')) e.textContent = values[index]; });
        const walker = document.createTreeWalker(document.body, NodeFilter.SHOW_TEXT);
        const nodes = []; while (walker.nextNode()) nodes.push(walker.currentNode);
        nodes.forEach(node => { const parent = node.parentElement; if (!parent || ['SCRIPT','STYLE','INPUT','TEXTAREA','OPTION'].includes(parent.tagName)) return; const raw = node.parentElement.dataset.i18nOriginal || node.textContent.trim(); if (!raw) return; const match = Object.entries(uiText).find(([source, values]) => source === raw || values.includes(raw)); if (match) { node.parentElement.dataset.i18nOriginal = raw; node.textContent = node.textContent.replace(raw, match[1][index]); } });
        document.querySelectorAll('[data-i18n]').forEach(e => { const values = keyedText[e.dataset.i18n]; if (values && !e.matches('.side-nav a')) e.textContent = values[index]; });
        document.querySelectorAll('input[placeholder],textarea[placeholder]').forEach(el => { for (const [source, values] of Object.entries(uiText)) if (el.getAttribute('placeholder').includes(source)) el.setAttribute('placeholder', el.getAttribute('placeholder').replace(source, values[index])); });
        document.querySelectorAll('option').forEach(option => { const raw = option.dataset.i18nOriginal || option.textContent.trim(); const match = Object.entries(uiText).find(([source, values]) => source === raw || values.includes(raw)); if (match) { option.dataset.i18nOriginal = raw; option.textContent = match[1][index]; } });
    };
    applyLanguage(saved);
    language.addEventListener('change', () => { localStorage.setItem('roombooking-language', language.value); applyLanguage(language.value); });
});

document.addEventListener('DOMContentLoaded', () => {
    const toggle = document.querySelector('.menu-toggle');
    if (!toggle) return;
    toggle.setAttribute('aria-expanded', 'true');
    toggle.addEventListener('click', () => {
        document.body.classList.toggle('sidebar-collapsed');
        toggle.setAttribute('aria-expanded', String(!document.body.classList.contains('sidebar-collapsed')));
    });
});

document.addEventListener('DOMContentLoaded', () => {
    const button = document.querySelector('.theme-toggle');
    if (!button) return;
    button.addEventListener('click', () => {
        document.documentElement.classList.add('theme-transitioning');
        const next = document.documentElement.dataset.theme === 'dark' ? 'light' : 'dark';
        document.documentElement.dataset.theme = next;
        button.setAttribute('aria-label', next === 'dark' ? 'เปลี่ยนเป็น Light mode' : 'เปลี่ยนเป็น Dark mode');
        localStorage.setItem('roombooking-theme-v2', next);
        window.setTimeout(() => document.documentElement.classList.remove('theme-transitioning'), 520);
    });
});

(function () {
  const translations = {
    th: {
      loginTitle: 'เข้าสู่ระบบ', registerTitle: 'สมัครสมาชิก', loginSubtitle: 'Meeting room reservation', registerSubtitle: 'สมัครบัญชีได้โดยไม่ต้องเลือกบริษัท',
      name: 'ชื่อ', email: 'อีเมล', password: 'รหัสผ่าน', confirmPassword: 'ยืนยันรหัสผ่าน', company: 'บริษัท', companyOptional: 'ไม่บังคับ', companyAccessHint: 'สมัครได้โดยไม่ต้องเลือกบริษัท — เลือกภายหลัง หรือใช้คำเชิญจากผู้ดูแลบริษัท', loginCompanyHint: '(บัญชีที่อยู่ในบริษัทแล้วต้องเลือกบริษัท)', registerCompanyHint: '(ไม่เลือกตอนนี้ได้)',
      chooseCompany: 'เลือกบริษัท', noCompany: 'ยังไม่เลือกบริษัท', remember: 'จดจำการเข้าสู่ระบบ', forgot: 'ลืมรหัสผ่าน?', login: 'เข้าสู่ระบบ', loading: 'กำลังเข้าสู่ระบบ...', createAccount: 'สร้างบัญชี',
      googleLogin: 'เข้าสู่ระบบด้วย Google', googleRegister: 'สมัครสมาชิกด้วย Google', or: 'หรือ', registerQuestion: 'ยังไม่มีบัญชีใช่ไหม?', register: 'สมัครสมาชิก', existingAccount: 'มีบัญชีแล้ว?', backToLogin: 'กลับไปเข้าสู่ระบบ', backToRegister: 'กลับไปสมัครสมาชิก', privacyTitle: 'นโยบายความเป็นส่วนตัว', termsTitle: 'ข้อกำหนดการใช้งาน', legalIntro: 'เอกสารนี้อธิบายหลักเกณฑ์การใช้งานระบบจองห้องประชุมของ Startbox', legalSectionAccount: 'บัญชีผู้ใช้', legalAccountText: 'ผู้ใช้ต้องให้ข้อมูลที่ถูกต้อง รักษารหัสผ่าน และรับผิดชอบกิจกรรมภายใต้บัญชีของตน', legalSectionData: 'ข้อมูลและความปลอดภัย', legalDataText: 'ระบบใช้ข้อมูลเท่าที่จำเป็นต่อการยืนยันตัวตน การจองห้อง และการตรวจสอบประวัติการใช้งาน', legalSectionContact: 'ติดต่อผู้ดูแลระบบ', legalContactText: 'หากมีคำถามเกี่ยวกับเอกสารนี้ กรุณาติดต่อผู้ดูแลระบบของบริษัท',
      support: 'มีปัญหาในการเข้าสู่ระบบ?', contact: 'ติดต่อผู้ดูแลระบบ', passwordHint: 'อย่างน้อย 8 ตัวอักษร ต้องมีตัวอักษรและตัวเลข', passwordRuleLength: 'อย่างน้อย 8 ตัวอักษร', passwordRuleLetter: 'มีตัวอักษร', passwordRuleNumber: 'มีตัวเลข', acceptTerms: 'ฉันยอมรับ', terms: 'ข้อกำหนดการใช้งาน', privacy: 'นโยบายความเป็นส่วนตัว', and: 'และ', termsRequired: 'กรุณายอมรับข้อกำหนดก่อนสมัคร', show: 'แสดง', hide: 'ซ่อน',
      emailRequired: 'กรุณากรอกอีเมล', passwordRequired: 'กรุณากรอกรหัสผ่าน', companyRequired: 'กรุณาเลือกบริษัท', invalidEmail: 'รูปแบบอีเมลไม่ถูกต้อง', invalidLogin: 'อีเมล รหัสผ่าน หรือบริษัทไม่ถูกต้อง', passwordMismatch: 'รหัสผ่านไม่ตรงกัน', companyAssignedLogin: 'บัญชีนี้อยู่ในบริษัทแล้ว กรุณาเลือกบริษัทก่อนเข้าสู่ระบบ', googleNotRegistered: 'ยังไม่พบบัญชี Google นี้ กรุณาสมัครสมาชิกก่อน', pendingApproval: 'สมัครสมาชิกสำเร็จแล้ว กรุณารอผู้ดูแลบริษัทอนุมัติบัญชี'
    },
    en: {
      loginTitle: 'Sign in', registerTitle: 'Create account', loginSubtitle: 'Meeting room reservation', registerSubtitle: 'Create an account without selecting a company',
      name: 'Name', email: 'Email', password: 'Password', confirmPassword: 'Confirm password', company: 'Company', companyOptional: 'Optional', companyAccessHint: 'You can register without a company — choose one later or use an invitation from a company administrator', loginCompanyHint: '(Required for accounts already assigned to a company)', registerCompanyHint: '(Optional for now)',
      chooseCompany: 'Select company', noCompany: 'No company selected', remember: 'Remember me', forgot: 'Forgot password?', login: 'Sign in', loading: 'Signing in...', createAccount: 'Create account',
      googleLogin: 'Sign in with Google', googleRegister: 'Sign up with Google', or: 'or', registerQuestion: "Don't have an account?", register: 'Sign up', existingAccount: 'Already have an account?', backToLogin: 'Back to sign in', backToRegister: 'Back to sign up', privacyTitle: 'Privacy Policy', termsTitle: 'Terms of use', legalIntro: 'This document explains the rules for using Startbox meeting room reservations.', legalSectionAccount: 'User accounts', legalAccountText: 'Users must provide accurate information, protect their password, and take responsibility for activity under their account.', legalSectionData: 'Data and security', legalDataText: 'The system uses only the information needed for identity verification, room reservations, and audit history.', legalSectionContact: 'Contact the administrator', legalContactText: 'For questions about this document, contact your company administrator.',
      support: 'Having trouble signing in?', contact: 'Contact administrator', passwordHint: 'At least 8 characters with letters and numbers', passwordRuleLength: 'At least 8 characters', passwordRuleLetter: 'Contains a letter', passwordRuleNumber: 'Contains a number', acceptTerms: 'I accept the', terms: 'Terms of use', privacy: 'Privacy Policy', and: 'and', termsRequired: 'Please accept the terms before signing up', show: 'Show', hide: 'Hide',
      emailRequired: 'Please enter your email', passwordRequired: 'Please enter your password', companyRequired: 'Please select a company', invalidEmail: 'Enter a valid email address', invalidLogin: 'The email, password, or company is incorrect', passwordMismatch: 'Passwords do not match', companyAssignedLogin: 'This account is already assigned to a company. Select the company before signing in.', googleNotRegistered: 'This Google account is not registered yet. Please sign up first.', pendingApproval: 'Registration completed. Please wait for company administrator approval.'
    },
    lo: {
      loginTitle: 'ເຂົ້າສູ່ລະບົບ', registerTitle: 'ສະໝັກສະມາຊິກ', loginSubtitle: 'ລະບົບຈອງຫ້ອງປະຊຸມ', registerSubtitle: 'ສ້າງບັນຊີໄດ້ໂດຍບໍ່ຕ້ອງເລືອກບໍລິສັດ',
      name: 'ຊື່', email: 'ອີເມວ', password: 'ລະຫັດຜ່ານ', confirmPassword: 'ຢືນຢັນລະຫັດຜ່ານ', company: 'ບໍລິສັດ', companyOptional: 'ບໍ່ບັງຄັບ', companyAccessHint: 'ສາມາດສະໝັກໄດ້ໂດຍບໍ່ເລືອກບໍລິສັດ — ເລືອກພາຍຫຼັງ ຫຼືໃຊ້ຄຳເຊີນຈາກຜູ້ດູແລບໍລິສັດ', loginCompanyHint: '(ບັນຊີທີ່ຢູ່ໃນບໍລິສັດແລ້ວຕ້ອງເລືອກບໍລິສັດ)', registerCompanyHint: '(ບໍ່ເລືອກຕອນນີ້ກໍໄດ້)',
      chooseCompany: 'ເລືອກບໍລິສັດ', noCompany: 'ຍັງບໍ່ເລືອກບໍລິສັດ', remember: 'ຈື່ຈຳການເຂົ້າລະບົບ', forgot: 'ລືມລະຫັດຜ່ານ?', login: 'ເຂົ້າສູ່ລະບົບ', loading: 'ກຳລັງເຂົ້າລະບົບ...', createAccount: 'ສ້າງບັນຊີ',
      googleLogin: 'ເຂົ້າສູ່ລະບົບດ້ວຍ Google', googleRegister: 'ສະໝັກດ້ວຍ Google', or: 'ຫຼື', registerQuestion: 'ຍັງບໍ່ມີບັນຊີ?', register: 'ສະໝັກສະມາຊິກ', existingAccount: 'ມີບັນຊີແລ້ວ?', backToLogin: 'ກັບໄປເຂົ້າສູ່ລະບົບ', backToRegister: 'ກັບໄປສະໝັກສະມາຊິກ', privacyTitle: 'ນະໂຍບາຍຄວາມສ່ວນຕົວ', termsTitle: 'ຂໍ້ກຳນົດການນຳໃຊ້', legalIntro: 'ເອກະສານນີ້ອະທິບາຍຫຼັກເກນການນຳໃຊ້ລະບົບຈອງຫ້ອງປະຊຸມຂອງ Startbox', legalSectionAccount: 'ບັນຊີຜູ້ໃຊ້', legalAccountText: 'ຜູ້ໃຊ້ຕ້ອງໃຫ້ຂໍ້ມູນຖືກຕ້ອງ ຮັກສາລະຫັດຜ່ານ ແລະຮັບຜິດຊອບກິດຈະກຳພາຍໃຕ້ບັນຊີຂອງຕົນ', legalSectionData: 'ຂໍ້ມູນ ແລະຄວາມປອດໄພ', legalDataText: 'ລະບົບໃຊ້ຂໍ້ມູນເທົ່າທີ່ຈຳເປັນຕໍ່ການຢືນຢັນຕົວຕົນ ການຈອງຫ້ອງ ແລະການກວດສອບປະຫວັດ', legalSectionContact: 'ຕິດຕໍ່ຜູ້ດູແລລະບົບ', legalContactText: 'ຖ້າມີຄຳຖາມກ່ຽວກັບເອກະສານນີ້ ກະລຸນາຕິດຕໍ່ຜູ້ດູແລລະບົບຂອງບໍລິສັດ',
      support: 'ມີບັນຫາໃນການເຂົ້າລະບົບ?', contact: 'ຕິດຕໍ່ຜູ້ດູແລລະບົບ', passwordHint: 'ຢ່າງໜ້ອຍ 8 ຕົວອັກສອນ ຕ້ອງມີຕົວອັກສອນ ແລະ ຕົວເລກ', passwordRuleLength: 'ຢ່າງໜ້ອຍ 8 ຕົວອັກສອນ', passwordRuleLetter: 'ມີຕົວອັກສອນ', passwordRuleNumber: 'ມີຕົວເລກ', acceptTerms: 'ຂ້ອຍຍອມຮັບ', terms: 'ຂໍ້ກຳນົດການນຳໃຊ້', privacy: 'ນະໂຍບາຍຄວາມສ່ວນຕົວ', and: 'ແລະ', termsRequired: 'ກະລຸນາຍອມຮັບຂໍ້ກຳນົດກ່ອນສະໝັກ', show: 'ສະແດງ', hide: 'ເຊື່ອງ',
      emailRequired: 'ກະລຸນາໃສ່ອີເມວ', passwordRequired: 'ກະລຸນາໃສ່ລະຫັດຜ່ານ', companyRequired: 'ກະລຸນາເລືອກບໍລິສັດ', invalidEmail: 'ຮູບແບບອີເມວບໍ່ຖືກຕ້ອງ', invalidLogin: 'ອີເມວ ລະຫັດຜ່ານ ຫຼືບໍລິສັດບໍ່ຖືກຕ້ອງ', passwordMismatch: 'ລະຫັດຜ່ານບໍ່ກົງກັນ', companyAssignedLogin: 'ບັນຊີນີ້ຢູ່ໃນບໍລິສັດແລ້ວ ກະລຸນາເລືອກບໍລິສັດກ່ອນເຂົ້າລະບົບ', googleNotRegistered: 'ຍັງບໍ່ພົບບັນຊີ Google ນີ້ ກະລຸນາສະໝັກກ່ອນ', pendingApproval: 'ສະໝັກສຳເລັດແລ້ວ ກະລຸນາລໍຖ້າຜູ້ດູແລບໍລິສັດອະນຸມັດ'
    }
  };
  const languages = { 'ไทย': 'th', English: 'en', 'ລາວ': 'lo' };
  const selector = document.getElementById('loginLanguage') || document.getElementById('registerLanguage') || document.getElementById('legalLanguage');
  if (!selector) return;

  function apply(lang) {
    const t = translations[lang] || translations.th;
    document.documentElement.lang = lang === 'lo' ? 'lo' : lang;
    document.querySelectorAll('[data-i18n]').forEach(element => {
      const value = t[element.dataset.i18n];
      if (value !== undefined) element.textContent = value;
    });
    const serverError = document.querySelector('.validation-summary');
    if (serverError) {
      const message = serverError.dataset.errorSource || serverError.textContent.trim();
      serverError.dataset.errorSource = message;
      const errorTranslations = [
        ['กรุณายอมรับข้อกำหนด', t.termsRequired],
        ['บัญชีนี้อยู่ในบริษัทแล้ว', t.companyAssignedLogin],
        ['ยังไม่พบบัญชี Google นี้', t.googleNotRegistered],
        ['สมัครสมาชิกสำเร็จแล้ว', t.pendingApproval],
        ['อีเมล รหัสผ่าน หรือบริษัทไม่ถูกต้อง', t.invalidLogin]
      ];
      const translated = errorTranslations.find(([source]) => message.includes(source));
      if (translated) serverError.textContent = translated[1];
    }
    const companyOption = document.querySelector('#OrganizationId option[value=""]');
    if (companyOption) companyOption.textContent = document.getElementById('registerForm') ? t.noCompany : t.chooseCompany;
    const passwordToggle = document.getElementById('passwordToggle');
    if (passwordToggle) {
      const password = document.getElementById('Password');
      passwordToggle.textContent = password?.type === 'text' ? t.hide : t.show;
      passwordToggle.setAttribute('aria-label', password?.type === 'text' ? t.hide : t.show);
    }
    window.authTranslations = t;
    window.loginTranslations = t;
    localStorage.setItem('loginLanguage', lang);
  }

  const saved = localStorage.getItem('loginLanguage') || 'th';
  selector.value = Object.keys(languages).find(label => languages[label] === saved) || 'ไทย';
  apply(saved);
  selector.addEventListener('change', event => apply(languages[event.target.value] || 'th'));

  document.getElementById('loginForm')?.addEventListener('submit', () => setTimeout(() => {
    const t = window.authTranslations || translations.th;
    const email = document.getElementById('Email');
    const password = document.getElementById('Password');
    if (email && password) {
      document.getElementById('emailError').textContent = email.value ? (email.validity.valid ? '' : t.invalidEmail) : t.emailRequired;
      document.getElementById('passwordError').textContent = password.value ? '' : t.passwordRequired;
    }
  }, 0), true);

})();

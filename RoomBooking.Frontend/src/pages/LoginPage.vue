<script setup>
import { ref } from 'vue'; import users from '../data/users.json'
const emit = defineEmits(['login']); const email = ref(''); const password = ref(''); const error = ref('')
function submit() { if (!email.value || !password.value) { error.value = 'กรุณากรอกอีเมลและรหัสผ่าน'; return } if (!users.some(u => u.email === email.value && u.password === password.value)) { error.value = 'อีเมลหรือรหัสผ่านไม่ถูกต้อง'; return } emit('login') }
</script>
<template>
    <div class="login-page">
        <form @submit.prevent="submit">
            <div class="logo">rb</div><small>ROOMBOOKING · MAKUB CENTER</small>
            <h1>เข้าสู่ระบบ</h1>
            <p>จัดการการจองห้องประชุมของคุณ</p><label>อีเมล<input v-model="email" type="email"
                    placeholder="name@company.com" /></label><label>รหัสผ่าน<input v-model="password" type="password"
                    placeholder="••••••••" /></label><button>เข้าสู่ระบบ</button><span v-if="error"
                class="error">{{ error }}</span>
            <footer>Demo: ใช้อีเมลและรหัสผ่านใดก็ได้</footer>
        </form>
    </div>
</template>
<style
    scoped>
    .login-page {
        min-height: 100vh;
        display: grid;
        place-items: center;
        background: #eef3fb
    }

    .login-page form {
        width: min(440px, calc(100% - 32px));
        background: #fff;
        border: 1px solid #d8e2f0;
        border-radius: 24px;
        padding: 42px;
        box-shadow: 0 18px 45px #48668d18
    }

    .logo {
        width: 58px;
        height: 58px;
        border-radius: 16px;
        background: #51c7b2;
        color: #fff;
        display: grid;
        place-items: center;
        font-weight: 800;
        font-size: 22px;
        margin-bottom: 26px
    }

    .login-page small {
        color: #81938f;
        font-weight: 800;
        letter-spacing: .12em
    }

    .login-page h1 {
        font-size: 34px;
        margin: 12px 0 8px
    }

    .login-page p {
        color: #71818c;
        margin-bottom: 30px
    }

    .login-page label {
        display: block;
        font-weight: 700;
        margin: 18px 0;
        color: #425264
    }

    .login-page label input {
        display: block;
        width: 100%;
        padding: 13px 14px;
        margin-top: 8px;
        border: 1px solid #d6e0ed;
        border-radius: 10px;
        font: inherit;
        box-sizing: border-box
    }

    .login-page button {
        width: 100%;
        margin-top: 18px;
        padding: 14px;
        border: 0;
        border-radius: 11px;
        background: #5579b7;
        color: #fff;
        font-weight: 800;
        font-size: 15px
    }

    .error {
        display: block;
        color: #c45353;
        margin-top: 12px;
        font-size: 13px
    }

    .login-page footer {
        display: block;
        text-align: center;
        color: #8a969e;
        font-size: 12px;
        margin-top: 22px
    }
</style>

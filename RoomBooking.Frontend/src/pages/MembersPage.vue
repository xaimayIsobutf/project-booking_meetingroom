<script setup>
import { ref } from 'vue'
import seed from '../data/users.json'
const members = ref(structuredClone(seed)), editing = ref(null)
const form = ref({ fullName: '', email: '', password: '1234', role: 'Employee', company: 'Acme Corporation', isActive: true })
function open(item) { editing.value = item?.id || 0; form.value = item ? { ...item } : { fullName: '', email: '', password: '1234', role: 'Employee', company: 'Acme Corporation', isActive: true } }
function save() { if (!form.value.fullName || !form.value.email) return; const item = { ...form.value, id: editing.value || Date.now() }; const index = members.value.findIndex(x => x.id === item.id); index < 0 ? members.value.push(item) : members.value[index] = item; editing.value = null }
function remove(id) { members.value = members.value.filter(x => x.id !== id) }
</script>
<template>
    <div class="page">
        <div class="heading">
            <div><small>MEMBER DIRECTORY</small>
                <h1>Member directory</h1>
                <p>ข้อมูลสมาชิกและสิทธิ์การใช้งานของบริษัท</p>
            </div><button class="primary" @click="open()">+ เพิ่มสมาชิก</button>
        </div>
        <section class="card">
            <div v-for="item in members" :key="item.id" class="member-row">
                <div class="identity"><span class="avatar">{{ item.fullName.charAt(0) }}</span>
                    <div><b>{{ item.fullName }}</b><small>{{ item.email }}</small></div>
                </div><span>{{ item.company }}</span><i>{{ item.role }}</i><span
                    :class="item.isActive ? 'active' : 'inactive'">{{ item.isActive ? 'Active' : 'Inactive' }}</span><button
                    @click="open(item)">แก้ไข</button><button class="delete" @click="remove(item.id)">ลบ</button>
            </div>
        </section>
        <div v-if="editing !== null" class="modal">
            <form class="form-card" @submit.prevent="save">
                <h2>{{ editing ? 'แก้ไข' : 'เพิ่ม' }}สมาชิก</h2>
                <label>ชื่อ-นามสกุล<input v-model="form.fullName"
                        required /></label><label>อีเมล<input v-model="form.email" type="email"
                        required /></label><label>รหัสผ่าน<input v-model="form.password" type="password"
                        required /></label><label>Role<select v-model="form.role">
                        <option>Employee</option>
                        <option>TenantAdmin</option>
                        <option>Approver</option>
                        <option>SuperAdmin</option>
                    </select></label><label>บริษัท<input v-model="form.company" required /></label><label
                    class="checkbox"><input v-model="form.isActive" type="checkbox" /> เปิดใช้งานสมาชิก</label>
                <div class="actions"><button type="button" @click="editing = null">ยกเลิก</button><button class="primary"
                        type="submit">บันทึก</button></div>
            </form>
        </div>
    </div>
</template>
<style scoped>
.page small {
    color: #81938f;
    font-weight: 800;
    letter-spacing: .12em
}

.heading {
    display: flex;
    justify-content: space-between;
    align-items: end;
    margin-bottom: 30px
}

.page h1 {
    font-size: 42px;
    margin: 10px 0
}

.primary {
    border: 0;
    border-radius: 10px;
    background: #5579b7;
    color: #fff;
    padding: 13px 18px;
    font-weight: 700
}

.card,
.form-card {
    background: #fff;
    border: 1px solid #d8e2f0;
    border-radius: 20px;
    padding: 28px
}

.member-row {
    display: grid;
    grid-template-columns: 2fr 1.4fr 1fr .8fr auto auto;
    gap: 16px;
    align-items: center;
    padding: 18px 0;
    border-bottom: 1px solid #e8edf4
}

.identity {
    display: flex;
    gap: 12px;
    align-items: center
}

.identity small {
    display: block;
    margin-top: 5px;
    color: #71818c;
    letter-spacing: 0;
    font-weight: 400
}

.avatar {
    width: 40px;
    height: 40px;
    border-radius: 50%;
    display: grid;
    place-items: center;
    background: #5e83c3;
    color: #fff;
    font-weight: 800
}

.member-row i {
    color: #5579b7;
    font-style: normal;
    font-weight: 700
}

.active {
    color: #3e9587;
    background: #e5f5f1;
    border-radius: 20px;
    padding: 6px 9px;
    font-size: 12px;
    text-align: center
}

.inactive {
    color: #c45353;
    background: #fdecec;
    border-radius: 20px;
    padding: 6px 9px;
    font-size: 12px;
    text-align: center
}

.member-row button,
.actions button {
    border: 1px solid #d6e0ed;
    background: #fff;
    border-radius: 8px;
    padding: 9px 12px
}

.member-row .delete {
    color: #c45353
}

.modal {
    position: fixed;
    inset: 0;
    background: #17243566;
    display: grid;
    place-items: center
}

.form-card {
    width: min(480px, calc(100% - 32px));
    box-sizing: border-box
}

.form-card h2 {
    margin-top: 0
}

.form-card label {
    display: block;
    color: #425264;
    font-weight: 700;
    margin: 12px 0
}

.form-card input:not([type=checkbox]),
.form-card select {
    display: block;
    width: 100%;
    box-sizing: border-box;
    padding: 12px;
    margin-top: 6px;
    border: 1px solid #d6e0ed;
    border-radius: 9px;
    font: inherit
}

.checkbox {
    display: flex !important;
    gap: 8px;
    align-items: center
}

.actions {
    display: flex;
    justify-content: flex-end;
    gap: 10px;
    margin-top: 20px
}

@media(max-width:900px) {
    .member-row {
        grid-template-columns: 1fr 1fr
    }

    .identity {
        grid-column: 1/-1
    }
}

@media(max-width:600px) {
    .heading {
        display: block
    }

    .heading .primary {
        margin-top: 16px
    }

    .member-row {
        grid-template-columns: 1fr
    }

    .member-row>* {
        grid-column: auto
    }
}
</style>

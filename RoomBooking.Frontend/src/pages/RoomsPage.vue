<script setup>
import { ref } from 'vue'; import seed from '../data/rooms.json'; const rooms = ref(structuredClone(seed)); const q = ref(''), editing = ref(null); const form = ref({ name: '', building: '', floor: '', capacity: 1, equipment: '', available: true }); function open(r) { editing.value = r?.id || 0; form.value = r ? { ...r } : { name: '', building: '', floor: 'ชั้น 1', capacity: 1, equipment: '', available: true } } function save() { const x = { ...form.value, id: editing.value || Date.now() }; const i = rooms.value.findIndex(r => r.id === x.id); i < 0 ? rooms.value.push(x) : rooms.value[i] = x; editing.value = null } function remove(id) { if (confirm('ยืนยันการลบห้องประชุมนี้?')) rooms.value = rooms.value.filter(r => r.id !== id) }
</script>
<template>
    <div class="page">
        <div class="heading">
            <div><small>MEETING ROOM DIRECTORY</small>
                <h1>Meeting room directory</h1>
                <p>ข้อมูลห้องประชุมของบริษัท</p>
            </div><button class="primary" @click="open()">+ เพิ่มห้องประชุม</button>
        </div>
        <section class="card"><input v-model="q" placeholder="ค้นหาชื่อห้อง อาคาร ชั้น หรืออุปกรณ์..." />
            <div class="table-head">
                <b>ห้องประชุม</b><b>อาคาร</b><b>ชั้น</b><b>อุปกรณ์ภายในห้อง</b><b>รองรับ</b><b>สถานะ</b><b>จัดการ</b>
            </div>
            <div v-for="r in rooms.filter(x => !q || `${x.name} ${x.building} ${x.floor} ${x.equipment}`.toLowerCase().includes(q.toLowerCase()))"
                :key="r.id" class="row">
                <b>{{ r.name }}</b><span>{{ r.building }}</span><span>{{ r.floor }}</span><span>{{ r.equipment }}</span><span>{{ r.capacity }}
                    คน</span><span :class="r.available ? 'on' : 'off'">{{ r.available ? 'เปิดใช้งาน' : 'ปิดใช้งาน' }}</span>
                <div><button @click="open(r)">แก้ไข</button><button class="delete" @click="remove(r.id)">ลบ</button>
                </div>
            </div>
        </section>
        <div v-if="editing !== null" class="modal">
            <form class="form" @submit.prevent="save">
                <h2>{{ editing ? 'แก้ไข' : 'เพิ่ม' }}ห้องประชุม</h2>
                <label>ชื่อห้องประชุม<small>ชื่อที่ผู้ใช้งานจะเห็น</small><input v-model="form.name" required
                        placeholder="เช่น Blue Ocean" /></label><label>อาคาร<small>สถานที่ตั้งของห้อง</small><input
                        v-model="form.building"
                        placeholder="เช่น อาคาร A" /></label><label>ชั้น<small>ชั้นที่ตั้งของห้อง</small><input
                        v-model="form.floor"
                        placeholder="เช่น ชั้น 1" /></label><label>จำนวนผู้รองรับ<small>จำนวนคนสูงสุด</small><input
                        v-model.number="form.capacity" type="number" min="1"
                        required /></label><label>อุปกรณ์ภายในห้อง<small>คั่นรายการด้วย comma</small><input
                        v-model="form.equipment"
                        placeholder="เช่น จอ, Video call" /></label><label>สถานะการใช้งาน<select
                        v-model="form.available">
                        <option :value="true">เปิดใช้งาน</option>
                        <option :value="false">ปิดใช้งาน</option>
                    </select></label>
                <div class="actions"><button type="button" @click="editing = null">ยกเลิก</button><button
                        class="primary">บันทึก</button></div>
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
    align-items: end
}

.page h1 {
    font-size: 42px
}

.primary {
    border: 0;
    border-radius: 10px;
    background: #5579b7;
    color: #fff;
    padding: 12px 16px;
    font-weight: 700
}

.card,
.form {
    background: #fff;
    border: 1px solid #d8e2f0;
    border-radius: 20px;
    padding: 30px;
    margin-top: 24px
}

.card>input,
.form input,
.form select {
    padding: 13px;
    border: 1px solid #d6e0ed;
    border-radius: 10px;
    font: inherit;
    width: 100%;
    box-sizing: border-box;
    margin: 5px 0
}

.table-head,
.row {
    display: grid;
    grid-template-columns: 1.4fr 1fr .8fr 1.8fr .8fr 1fr 1.1fr;
    gap: 14px;
    align-items: center
}

.table-head {
    padding: 20px 0;
    color: #71818c;
    border-bottom: 1px solid #dfe7f0
}

.row {
    padding: 20px 0;
    border-bottom: 1px solid #e8edf4
}

.on,
.off {
    padding: 6px 9px;
    border-radius: 20px;
    text-align: center;
    font-size: 12px
}

.on {
    color: #3e9587;
    background: #e5f5f1
}

.off {
    color: #c45353;
    background: #fdecec
}

.row button,
.actions button {
    border: 1px solid #d6e0ed;
    background: #fff;
    border-radius: 8px;
    padding: 8px 10px
}

.delete {
    color: #c45353 !important
}

.modal {
    position: fixed;
    inset: 0;
    background: #17243566;
    display: grid;
    place-items: center
}

.form {
    width: min(480px, calc(100% - 32px));
    margin: 0
}

.form h2 {
    margin-top: 0
}

.form label {
    display: block;
    color: #425264;
    font-weight: 700;
    margin: 10px 0
}

.form label small {
    display: block;
    letter-spacing: 0;
    font-weight: 400;
    margin-top: 3px
}

.actions {
    display: flex;
    justify-content: flex-end;
    gap: 10px;
    margin-top: 18px
}

@media(max-width:900px) {
    .table-head {
        display: none
    }

    .row {
        grid-template-columns: 1fr 1fr
    }

    .row b {
        grid-column: 1/-1
    }
}
</style>

<script setup>
import { ref, computed } from 'vue'
import seed from '../data/reservations.json'
const reservations = ref(structuredClone(seed)), editing = ref(null), query = ref(''), filter = ref('all'), calendarDate = ref('2026-08-25')
const calendarRooms = ['Blue Ocean']; const calendarSlots = [['08:00', '10:00'], ['10:00', '12:00'], ['13:00', '15:00'], ['15:00', '17:00']]
function isBooked(room, start) { return reservations.value.some(x => x.date === calendarDate.value && x.room === room && x.start === start && x.status !== 'Cancelled') }
function bookSlot(room, start, end) { open(); form.value.room = room; form.value.date = calendarDate.value; form.value.period = `${start}-${end}`; form.value.start = start; form.value.end = end }
const form = ref({ subject: '', room: 'Blue Ocean', user: 'Acme Admin', date: '', period: '2h-1', status: 'Pending', start: '08:00', end: '10:00' })
const slots = { full: { label: 'เต็มวัน', start: '08:00', end: '17:00' }, morning: { label: 'ครึ่งวันเช้า', start: '08:00', end: '12:00' }, afternoon: { label: 'ครึ่งวันบ่าย', start: '13:00', end: '17:00' }, '2h-1': { label: '08:00 - 10:00', start: '08:00', end: '10:00' }, '2h-2': { label: '10:00 - 12:00', start: '10:00', end: '12:00' }, '2h-3': { label: '13:00 - 15:00', start: '13:00', end: '15:00' }, '2h-4': { label: '15:00 - 17:00', start: '15:00', end: '17:00' } }
const visible = computed(() => reservations.value.filter(x => (filter.value === 'all' || x.status === filter.value) && (!query.value || `${x.subject} ${x.room} ${x.user}`.toLowerCase().includes(query.value.toLowerCase()))))
function setPeriod() { const s = slots[form.value.period]; form.value.start = s.start; form.value.end = s.end }
function open(item) { editing.value = item?.id || 0; form.value = item ? { ...item, period: item.period || '2h-1' } : { subject: '', room: 'Blue Ocean', user: 'Acme Admin', date: '', period: '2h-1', status: 'Pending', start: '08:00', end: '10:00' } }
function save() { if (!form.value.subject || !form.value.date) return; const item = { ...form.value, id: editing.value || Date.now(), periodLabel: slots[form.value.period].label }; const i = reservations.value.findIndex(x => x.id === item.id); i < 0 ? reservations.value.push(item) : reservations.value[i] = item; editing.value = null }
function remove(id) { reservations.value = reservations.value.filter(x => x.id !== id) }
function checkIn(item) { item.status = 'CheckedIn' }
function checkOut(item) { item.status = 'Completed' }
</script>
<template>
    <div class="page">
        <div class="heading">
            <div><small>RESERVATION MANAGEMENT</small>
                <h1>ตารางการจองห้อง</h1>
                <p>เลือกวันที่และช่วงเวลาที่ต้องการจองห้องประชุม</p>
            </div><button class="primary" @click="open()">+ จองห้องประชุม</button>
        </div>
        <section class="toolbar"><input v-model="query" placeholder="ค้นหาหัวข้อ ห้อง หรือผู้จอง..." /><select
                v-model="filter">
                <option value="all">ทุกสถานะ</option>
                <option>Pending</option>
                <option>Approved</option>
                <option>Rejected</option>
                <option>Cancelled</option>
            </select></section>
        <section class="card">
            <div v-for="item in visible" :key="item.id" class="reservation-row">
                <div><b>{{ item.subject }}</b><small>{{ item.room }} · {{ item.date }}</small><small>{{ item.periodLabel ||
                    `${item.start} - ${item.end}`}} · {{ item.user }}</small></div><span
                    class="status">{{ item.status }}</span><button v-if="item.status === 'Approved'" class="check-in" @click="checkIn(item)">เช็กอิน</button><button v-if="item.status === 'CheckedIn'" class="check-out" @click="checkOut(item)">เช็กเอาต์</button><button @click="open(item)">แก้ไข</button><button
                    class="delete" @click="remove(item.id)">ลบ</button>
            </div>
        </section>
        <div v-if="editing !== null" class="modal">
            <form class="form-card" @submit.prevent="save">
                <h2>{{ editing ? 'แก้ไข' : 'เพิ่ม' }}การจอง</h2><label>หัวข้อ<input v-model="form.subject" required
                        placeholder="หัวข้อการประชุม" /></label><label>ห้องประชุม<input v-model="form.room"
                        required /></label><label>ผู้จอง<input v-model="form.user"
                        required /></label><label>วันที่<input v-model="form.date" type="date"
                        required /></label><label>รูปแบบการจอง<select v-model="form.period" @change="setPeriod">
                        <optgroup label="เต็มวัน / ครึ่งวัน">
                            <option value="full">เต็มวัน · 08:00 - 17:00</option>
                            <option value="morning">ครึ่งวันเช้า · 08:00 - 12:00</option>
                            <option value="afternoon">ครึ่งวันบ่าย · 13:00 - 17:00</option>
                        </optgroup>
                        <optgroup label="รอบ 2 ชั่วโมง">
                            <option value="2h-1">08:00 - 10:00</option>
                            <option value="2h-2">10:00 - 12:00</option>
                            <option value="2h-3">13:00 - 15:00</option>
                            <option value="2h-4">15:00 - 17:00</option>
                        </optgroup>
                    </select></label>
                <div class="time-preview">เวลาที่เลือก: <b>{{ form.start }} - {{ form.end }}</b></div><label>สถานะ<select
                        v-model="form.status">
                        <option>Pending</option>
                        <option>Approved</option>
                        <option>Rejected</option>
                        <option>Cancelled</option>
                    </select></label>
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

.toolbar {
    display: flex;
    gap: 12px;
    margin-bottom: 20px
}

.toolbar input,
.toolbar select {
    padding: 13px;
    border: 1px solid #d6e0ed;
    border-radius: 10px;
    font: inherit;
    background: #fff
}

.toolbar input {
    flex: 1
}

.card,
.form-card {
    background: #fff;
    border: 1px solid #d8e2f0;
    border-radius: 20px;
    padding: 28px
}

.reservation-row {
    display: flex;
    gap: 16px;
    align-items: center;
    padding: 20px 0;
    border-bottom: 1px solid #e8edf4
}

.reservation-row>div {
    flex: 1
}

.reservation-row small {
    display: block;
    margin-top: 6px;
    color: #71818c;
    letter-spacing: 0;
    font-weight: 400
}

.status {
    color: #3e9587;
    background: #e5f5f1;
    border-radius: 20px;
    padding: 7px 11px;
    font-size: 12px;
    font-weight: 700
}

.reservation-row button,
.actions button {
    border: 1px solid #d6e0ed;
    background: #fff;
    border-radius: 8px;
    padding: 9px 12px
}

.reservation-row .delete {
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
    width: min(520px, calc(100% - 32px));
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

.form-card input,
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

.time-preview {
    background: #e8f7f3;
    color: #2f7f70;
    padding: 12px;
    border-radius: 9px
}

.actions {
    display: flex;
    justify-content: flex-end;
    gap: 10px;
    margin-top: 20px
}

@media(max-width:700px) {
    .heading {
        display: block
    }

    .heading .primary {
        margin-top: 16px
    }

    .toolbar {
        display: block
    }

    .toolbar>* {
        width: 100%;
        margin-bottom: 10px
    }

    .reservation-row {
        flex-wrap: wrap
    }

    .reservation-row>div {
        min-width: 100%
    }
}
</style>

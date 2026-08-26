<script setup>
import { ref } from 'vue'
import rooms from '../data/rooms.json'
import reservations from '../data/reservations.json'
const date = ref('2026-08-25'); 
const slots = [['08:00', '10:00'], ['10:00', '12:00'], ['13:00', '15:00'], ['15:00', '17:00']]
function booked(room, start) { return reservations.some(x => x.date === date.value && x.room === room.name && x.start === start && x.status !== 'Cancelled') }
</script>
<template>
    <div class="page"><small>ROOM AVAILABILITY</small>
        <h1>Calendar Room</h1>
        <p>ตรวจสอบห้องว่างตามวันและช่วงเวลาที่ต้องการจอง</p>
        <section class="card">
            <div class="toolbar"><label>เลือกวันที่<input v-model="date" type="date" /></label><span><i
                        class="free-dot"></i> ว่าง <i class="busy-dot"></i> จองแล้ว</span></div>
            <div class="calendar">
                <div class="calendar-row header"><b>ห้องประชุม</b><b v-for="slot in slots" :key="slot[0]">{{ slot[0] }} -
                        {{ slot[1] }}</b></div>
                <div v-for="room in rooms" :key="room.id" class="calendar-row">
                    <div><b>{{ room.name }}</b><small>{{ room.capacity }} คน · {{ room.building }}</small></div>
                    <div v-for="slot in slots" :key="slot[0]" :class="['slot', booked(room, slot[0]) ? 'busy' : 'free']">
                        {{ booked(room, slot[0]) ? 'จองแล้ว' : 'ว่าง' }}</div>
                </div>
            </div>
        </section>
    </div>
</template>
<style scoped>
.page small {
    color: #81938f;
    font-weight: 800;
    letter-spacing: .12em
}

.page h1 {
    font-size: 42px;
    margin: 10px 0
}

.card {
    background: #fff;
    border: 1px solid #d8e2f0;
    border-radius: 20px;
    padding: 28px;
    margin-top: 28px
}

.toolbar {
    display: flex;
    justify-content: space-between;
    align-items: end;
    margin-bottom: 24px
}

.toolbar label {
    display: grid;
    gap: 7px;
    font-weight: 700
}

.toolbar input {
    padding: 12px;
    border: 1px solid #d6e0ed;
    border-radius: 9px;
    font: inherit
}

.toolbar span {
    color: #71818c
}

.free-dot,
.busy-dot {
    display: inline-block;
    width: 10px;
    height: 10px;
    border-radius: 50%;
    margin-left: 14px
}

.free-dot {
    background: #51c7b2
}

.busy-dot {
    background: #e58b91
}

.calendar-row {
    display: grid;
    grid-template-columns: 1.5fr repeat(4, 1fr);
    gap: 12px;
    align-items: center;
    border-top: 1px solid #e8edf4;
    padding: 16px 0
}

.calendar-row.header {
    border-top: 0;
    color: #71818c
}

.calendar-row small {
    display: block;
    letter-spacing: 0;
    margin-top: 5px;
    color: #81909b;
    font-weight: 400
}

.slot {
    border-radius: 10px;
    padding: 14px;
    text-align: center;
    font-size: 13px;
    font-weight: 700
}

.slot.free {
    background: #e5f5f1;
    color: #3e9587
}

.slot.busy {
    background: #fdecec;
    color: #c45353
}

@media(max-width:700px) {
    .toolbar {
        display: block
    }

    .toolbar span {
        display: block;
        margin-top: 16px
    }

    .calendar {
        overflow-x: auto
    }

    .calendar-row {
        min-width: 700px
    }
}
</style>

<script setup>
import { ref } from 'vue'; import roomsData from '../data/rooms.json'; import reservationsData from '../data/reservations.json'; defineEmits(['navigate']); const query = ref(''); const rooms = roomsData.map(r => [r.name, `${r.building} · ${r.floor} · ${r.capacity} คน`, r.available ? 'ว่าง' : 'กำลังใช้งาน']); const bookings = reservationsData.map(r => [r.subject, `${r.room} · วันนี้ ${r.start} - ${r.end}`, r.status])
</script>
<template>
    <div class="page">
        <div class="heading">
            <div><small>ROOMBOOKING · MAKUB CENTER</small>
                <h1>ภาพรวมการจองห้องประชุมของบริษัท</h1>
            </div><button @click="$emit('navigate', 'Reservations')">+ จองห้องประชุม</button>
        </div>
        <div class="metrics">
            <article><span>ห้องประชุมทั้งหมด</span><strong>3</strong><small>ห้อง</small></article>
            <article><span>การจองวันนี้</span><strong>2</strong><small>รายการ</small></article>
            <article class="available"><span>ห้องที่ว่างตอนนี้</span><strong>2</strong><small>พร้อมใช้งาน</small>
            </article>
        </div>
        <div class="grid">
            <article class="card">
                <h2>การจองวันนี้</h2><input v-model="query" placeholder="ค้นหาการจองหรือห้องประชุม..." />
                <div v-for="b in bookings" :key="b[0]" class="row"><b>{{ b[0] }}</b><small>{{ b[1] }}</small><i>{{ b[2] }}</i>
                </div>
            </article>
            <article class="card">
                <h2>ห้องประชุม</h2>
                <div v-for="r in rooms" :key="r[0]" class="row"><b>{{ r[0] }}</b><small>{{ r[1] }}</small><i>{{ r[2] }}</i>
                </div>
            </article>
        </div>
    </div>
</template>
<style
    scoped>
    .heading {
        display: flex;
        justify-content: space-between;
        align-items: end;
        margin-bottom: 38px
    }

    .heading small {
        color: #81938f;
        font-weight: 800;
        letter-spacing: .12em
    }

    .heading h1 {
        font-size: 42px;
        letter-spacing: -.04em;
        margin: 10px 0
    }

    .heading button {
        border: 0;
        border-radius: 11px;
        background: #5579b7;
        color: #fff;
        padding: 15px 22px;
        font-weight: 800
    }

    .metrics {
        display: grid;
        grid-template-columns: repeat(3, 1fr);
        gap: 24px;
        margin-bottom: 28px
    }

    .metrics article,
    .card {
        background: #fff;
        border: 1px solid #d8e2f0;
        border-radius: 20px
    }

    .metrics article {
        padding: 26px
    }

    .metrics span,
    .metrics small {
        display: block;
        color: #74869a
    }

    .metrics strong {
        display: block;
        font-size: 48px;
        margin: 10px 0
    }

    .available {
        background: #e8f7f3 !important
    }

    .available span,
    .available small {
        color: #2f7f70 !important
    }

    .grid {
        display: grid;
        grid-template-columns: 1.15fr .85fr;
        gap: 28px
    }

    .card {
        padding: 30px
    }

    .card h2 {
        margin-top: 0
    }

    .card input {
        width: 100%;
        padding: 13px;
        border: 1px solid #d6e0ed;
        border-radius: 10px;
        font: inherit
    }

    .row {
        position: relative;
        padding: 18px 0;
        border-bottom: 1px solid #e8edf4
    }

    .row b,
    .row small {
        display: block
    }

    .row small {
        margin-top: 5px;
        color: #71818c
    }

    .row i {
        position: absolute;
        right: 0;
        top: 28px;
        background: #e5f5f1;
        color: #3e9587;
        border-radius: 20px;
        padding: 6px 10px;
        font-style: normal;
        font-size: 12px;
        font-weight: 700
    }

    @media(max-width:800px) {
        .heading {
            display: block
        }

        .heading h1 {
            font-size: 32px
        }

        .heading button {
            margin-top: 18px
        }

        .metrics,
        .grid {
            grid-template-columns: 1fr
        }
    }
</style>

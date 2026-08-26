<script setup>
import { ref } from 'vue'; import LoginPage from './pages/LoginPage.vue'; import DashboardPage from './pages/DashboardPage.vue'; import RoomsPage from './pages/RoomsPage.vue'; import ReservationsPage from './pages/ReservationsPage.vue'; import MembersPage from './pages/MembersPage.vue'; import CalendarRoomPage from './pages/CalendarRoomPage.vue'; const loggedIn = ref(false); const page = ref('Dashboard'); const sidebarOpen = ref(true)
</script>
<template>
    <LoginPage v-if="!loggedIn" @login="loggedIn = true" />
    <div v-else :class="['app-shell',{'sidebar-collapsed':!sidebarOpen}]">
        <aside><b>RoomBooking</b><small>Meeting room center</small>
            <nav><button v-for="x in ['Dashboard', 'Meeting rooms', 'Calendar Room', 'Reservations', 'Members']" :class="{ active: page === x }"
                    @click="page = x">▦ {{ x }}</button></nav><button class="logout"
                @click="loggedIn = false">ออกจากระบบ</button>
        </aside>
        <section>
            <header><button class="sidebar-toggle" @click="sidebarOpen=!sidebarOpen">☰</button><small>Workspace</small>
                <h2>{{ page }}</h2>
            </header>
            <main>
                <DashboardPage v-if="page === 'Dashboard'" @navigate="page = $event" />
                <RoomsPage v-else-if="page === 'Meeting rooms'" />
                <CalendarRoomPage v-else-if="page === 'Calendar Room'" />
                <ReservationsPage v-else-if="page === 'Reservations'" />
                <MembersPage v-else />
            </main>
        </section>
    </div>
</template>
<style>
body {
    margin: 0;
    background: #eef3fb;
    color: #1d2a3c;
    font-family: Inter, system-ui, sans-serif
}

.app-shell {
    display: flex;
    min-height: 100vh
}

.app-shell aside {
    width: 280px;
    background: #172435;
    color: #fff;
    padding: 36px 22px;
    display: flex;
    flex-direction: column
}

.app-shell aside>b {
    font-size: 22px
}

.app-shell aside small {
    color: #a6b8c7;
    margin: 6px 0 50px
}

.app-shell nav {
    display: grid;
    gap: 8px
}

.app-shell nav button,
.logout {
    border: 0;
    background: none;
    color: #c9d4df;
    text-align: left;
    padding: 15px;
    border-radius: 12px;
    font: inherit;
    font-weight: 700
}

.app-shell nav button.active,
.app-shell nav button:hover {
    background: #304c78;
    color: #fff
}

.logout {
    margin-top: auto;
    border: 1px solid #35506d
}

.app-shell section {
    flex: 1
}

.app-shell header {
    height: 112px;
    background: #fff;
    border-bottom: 1px solid #dce4ef;
    padding: 24px 46px;
    box-sizing: border-box
}

.app-shell header small {
    color: #81938f
}

.app-shell header h2 {
    margin: 5px 0;
    font-size: 28px
}

.app-shell main {
    padding: 52px 46px
}
</style>
<style>
/* Soft UI Dashboard theme overrides for the RoomBooking application */
:root{--softui-bg:#f8f9fa;--softui-surface:#fff;--softui-ink:#344767;--softui-muted:#67748e;--softui-accent:#cb0c9f;--softui-border:rgba(52,71,103,.12)}
body{background:var(--softui-bg)!important;color:var(--softui-ink)!important}
.app-shell{background:var(--softui-bg)!important;color:var(--softui-ink)!important}
.app-shell:before,.app-shell:after{display:none!important}
.app-shell>aside{background:#102522!important;border-right:0!important;box-shadow:12px 0 32px rgba(13,30,28,.08);backdrop-filter:none!important}
.app-shell>aside>b{color:#fff!important}.app-shell>aside small{color:#a8c5bf!important}
.app-shell nav button,.app-shell .logout{color:#c7ddd8!important}
.app-shell nav button.active,.app-shell nav button:hover{background:linear-gradient(135deg,#1a7a6e,#2a9d8f)!important;color:#fff!important;box-shadow:0 8px 20px rgba(26,122,110,.2)}
.app-shell .logout{border-color:rgba(168,197,191,.2)!important}
.app-shell>section{background:var(--softui-bg)!important}
.app-shell>section>header{background:rgba(255,255,255,.92)!important;border-bottom:1px solid var(--softui-border)!important;color:var(--softui-ink)!important;backdrop-filter:blur(16px)!important}
.app-shell>section>header h2,.page h1,.page h2,.page h3,.page p,.page b,.page span{color:var(--softui-ink)!important}
.page small{color:var(--softui-muted)!important}
.page .card,.page .metrics article{background:rgba(255,255,255,.92)!important;border:1px solid var(--softui-border)!important;box-shadow:0 10px 24px rgba(52,71,103,.08)!important;color:var(--softui-ink)!important;backdrop-filter:blur(12px)!important}
.page input,.page select,.form input,.form select{background:#fff!important;color:var(--softui-ink)!important;border-color:var(--softui-border)!important}
.page input::placeholder{color:#8ba5a0!important}.primary{background:linear-gradient(135deg,#1a7a6e,#2a9d8f)!important;color:#fff!important}
.on,.free,.status{background:rgba(42,157,143,.13)!important;color:#1a7a6e!important}.off,.booked{background:rgba(190,70,70,.12)!important;color:#a33d3d!important}
</style>
<style>
/* Clearwave readability and layout polish */
.app-shell nav button,.app-shell .logout{color:#c7ddd8!important;opacity:1!important}
.app-shell nav button.active,.app-shell nav button:hover{color:#fff!important}
.app-shell>aside small{opacity:1!important}
.page{max-width:1400px;margin:0 auto}
.page .card{overflow:hidden}
.page button{transition:transform .2s ease,box-shadow .2s ease}
.page button:hover{transform:translateY(-1px)}
.modal{background:rgba(13,30,28,.42)!important;backdrop-filter:blur(5px)}
.form-card{background:#fff!important;color:#0d1e1c!important;border:1px solid rgba(13,30,28,.12)!important;box-shadow:0 24px 64px rgba(13,30,28,.2)!important}
.form-card h2,.form-card label,.form-card b{color:#0d1e1c!important}
.form-card input,.form-card select{display:block;width:100%;min-height:48px;margin-top:8px;padding:12px 14px;background:#fff!important;color:#0d1e1c!important;border:1px solid #cbded9!important;border-radius:12px!important;opacity:1!important}
.form-card input::placeholder{color:#8aa19d!important}
.time-preview{background:#e8f4f2!important;color:#1a7a6e!important;border:1px solid rgba(26,122,110,.14)!important}
</style>
<style>
.sidebar-toggle{width:42px!important;height:42px!important;margin-right:14px;cursor:pointer;color:#5579b7!important;background:#f5f8fc!important;font-size:20px!important}.header-title{display:flex;align-items:center}.sidebar-collapsed .sidebar{width:0;min-width:0;padding-left:0;padding-right:0;overflow:hidden}.sidebar-collapsed .workspace{width:100%}
</style>
<style>
.sidebar-toggle{width:42px;height:42px;margin-right:16px;border:1px solid #d9e2eb;background:#fff;border-radius:10px;color:#5579b7;font-size:19px;cursor:pointer}.sidebar-collapsed aside{width:0;min-width:0;padding-left:0;padding-right:0;overflow:hidden}.sidebar-collapsed section{width:100%}
</style>
<style>
.sidebar-collapsed aside{width:76px!important;min-width:76px!important;padding:28px 12px!important;align-items:center;transition:width .2s}.sidebar-collapsed aside>b,.sidebar-collapsed aside>small,.sidebar-collapsed aside .logout{font-size:0;overflow:hidden;white-space:nowrap}.sidebar-collapsed aside>b:before{content:'rb';font-size:17px;display:grid;place-items:center;width:42px;height:42px;background:#51c7b2;border-radius:12px;color:#fff}.sidebar-collapsed aside nav{width:100%;margin-top:28px}.sidebar-collapsed aside nav button{font-size:0;width:52px;height:52px;padding:0;text-align:center;display:grid;place-items:center;margin:0 auto;border-radius:14px}.sidebar-collapsed aside nav button:before{content:'▦';font-size:22px;color:#c9d4df}.sidebar-collapsed aside nav button:nth-child(2):before{content:'▥'}.sidebar-collapsed aside nav button:nth-child(3):before{content:'◷'}.sidebar-collapsed aside nav button:nth-child(4):before{content:'⌑'}.sidebar-collapsed aside nav button:nth-child(5):before{content:'♙'}.sidebar-collapsed aside nav button.active:before{color:#fff}.sidebar-collapsed aside nav button.active{background:#304c78;box-shadow:inset 4px 0 #5d89dc}.sidebar-collapsed aside .logout{display:block;width:52px;height:52px;margin-top:auto;padding:0}.sidebar-collapsed aside .logout:before{content:'⇥';font-size:22px;color:#b9c7d3}
</style>

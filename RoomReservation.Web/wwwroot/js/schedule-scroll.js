(() => {
    const wrapSchedule = () => {
        const schedule = document.querySelector('.schedule-board > .resource-schedule, .schedule-board > .week-time-schedule');
        if (!schedule || schedule.parentElement?.classList.contains('schedule-scroll')) return;

        const wrapper = document.createElement('div');
        wrapper.className = 'schedule-scroll';
        schedule.before(wrapper);
        wrapper.append(schedule);
    };

    if (document.readyState === 'loading') {
        document.addEventListener('DOMContentLoaded', wrapSchedule, { once: true });
    } else {
        wrapSchedule();
    }
})();

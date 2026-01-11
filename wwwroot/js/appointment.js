// wwwroot/js/appointment.js
// Улучшенная версия с валидацией времени

// Обновление цены и длительности при выборе услуги
function updateServiceDetails(select) {
    if (!select) return;

    const selectedOption = select.options[select.selectedIndex];
    const priceAttr = selectedOption.getAttribute('data-price');
    const durationAttr = selectedOption.getAttribute('data-duration');

    let price = 0;
    let duration = 60;

    // Парсим числовые значения
    if (priceAttr) {
        price = parseFloat(priceAttr);
        if (isNaN(price)) price = 0;
    }

    if (durationAttr) {
        duration = parseInt(durationAttr);
        if (isNaN(duration)) duration = 60;
    }

    const priceInput = document.getElementById('priceInput');
    const durationInput = document.getElementById('durationInput');

    if (priceInput) priceInput.value = price.toFixed(2);
    if (durationInput) durationInput.value = duration;
}

// Загрузка настроек времени
async function loadTimeSettings() {
    try {
        // Используем прямой путь к методу
        const response = await fetch('/TimeSettings?handler=TimeSettings');
        if (!response.ok) {
            throw new Error('Network response was not ok');
        }

        return await response.json();
    } catch (error) {
        console.error('Error loading time settings:', error);
        return {
            interval: 20,
            workStart: '09:00',
            workEnd: '21:00'
        };
    }
}

// Генерация временных слотов
function generateTimeSlots(interval, workStart, workEnd, minTime = null) {
    const timeSelect = document.getElementById('timeSelect');
    if (!timeSelect) return;

    timeSelect.innerHTML = '<option value="">Выберите время</option>';

    // Парсим время начала и конца
    const [startHour, startMinute] = workStart.split(':').map(Number);
    const [endHour, endMinute] = workEnd.split(':').map(Number);

    const today = new Date();
    const dateInput = document.getElementById('dateInput');
    const selectedDate = dateInput ? new Date(dateInput.value + 'T00:00:00') : today;

    // Определяем минимальное время для записи
    let minDateTime = minTime;
    if (!minDateTime) {
        if (selectedDate.toDateString() === today.toDateString()) {
            // Если сегодня, минимальное время = текущее время + 15 минут
            minDateTime = new Date();
            minDateTime.setMinutes(minDateTime.getMinutes() + 15);
        } else {
            // Если не сегодня, минимальное время = начало рабочего дня
            minDateTime = new Date(selectedDate);
            minDateTime.setHours(startHour, startMinute, 0, 0);
        }
    }

    const startTime = new Date(selectedDate);
    startTime.setHours(startHour, startMinute, 0, 0);

    const endTime = new Date(selectedDate);
    endTime.setHours(endHour, endMinute, 0, 0);

    // Генерируем слоты
    let currentTime = new Date(startTime);
    let hasAvailableSlots = false;

    while (currentTime < endTime) {
        // Пропускаем прошедшие времена
        if (currentTime >= minDateTime) {
            const hours = currentTime.getHours().toString().padStart(2, '0');
            const minutes = currentTime.getMinutes().toString().padStart(2, '0');
            const timeString = `${hours}:${minutes}`;

            const option = document.createElement('option');
            option.value = timeString;
            option.textContent = timeString;

            // Добавляем подсказку если время близко к текущему
            if (selectedDate.toDateString() === today.toDateString()) {
                const now = new Date();
                const timeDiff = (currentTime - now) / (1000 * 60); // разница в минутах
                if (timeDiff < 30) {
                    option.textContent += ' (скоро)';
                }
            }

            timeSelect.appendChild(option);
            hasAvailableSlots = true;
        }

        // Добавляем интервал
        currentTime.setMinutes(currentTime.getMinutes() + interval);
    }

    if (!hasAvailableSlots) {
        const option = document.createElement('option');
        option.value = "";
        option.textContent = "Нет доступного времени";
        option.disabled = true;
        timeSelect.appendChild(option);
    }
}

// Загрузка временных слотов
async function loadTimeSlots() {
    const settings = await loadTimeSettings();

    // Показываем кратность
    const intervalValue = document.getElementById('intervalValue');
    const timeInfo = document.getElementById('timeInfo');

    if (intervalValue) {
        intervalValue.textContent = settings.interval;
    }

    if (timeInfo) {
        timeInfo.innerHTML =
            `Кратность: <span class="badge bg-primary">${settings.interval} мин</span> | ` +
            `Рабочие часы: ${settings.workStart} - ${settings.workEnd}`;
    }

    // Генерируем временные слоты
    generateTimeSlots(settings.interval, settings.workStart, settings.workEnd);
}

// Обновление скрытого поля даты-времени
function updateDateTime() {
    const dateInput = document.getElementById('dateInput');
    const timeSelect = document.getElementById('timeSelect');
    const startTimeInput = document.getElementById('startTimeInput');

    if (dateInput && timeSelect && startTimeInput && dateInput.value && timeSelect.value) {
        startTimeInput.value = dateInput.value + 'T' + timeSelect.value + ':00';
    }
}

// Проверка доступности времени
function validateDateTime() {
    const dateInput = document.getElementById('dateInput');
    const timeSelect = document.getElementById('timeSelect');

    if (!dateInput || !timeSelect || !dateInput.value || !timeSelect.value) {
        return { isValid: false, message: "Укажите дату и время" };
    }

    const selectedDateTime = new Date(dateInput.value + 'T' + timeSelect.value + ':00');
    const now = new Date();

    // Проверяем, что время не в прошлом
    if (selectedDateTime < now) {
        return {
            isValid: false,
            message: `Нельзя записать на прошедшее время. Сейчас ${now.toLocaleTimeString([], { hour: '2-digit', minute: '2-digit' })}`
        };
    }

    // Проверяем, что время не слишком близко к текущему (минимум 15 минут)
    const timeDiff = (selectedDateTime - now) / (1000 * 60); // разница в минутах
    if (timeDiff < 15) {
        return {
            isValid: false,
            message: `Запись должна быть минимум на 15 минут позже текущего времени. Сейчас ${now.toLocaleTimeString([], { hour: '2-digit', minute: '2-digit' })}`
        };
    }

    return { isValid: true };
}

// Инициализация формы создания/редактирования
function initializeAppointmentForm() {
    const serviceSelect = document.getElementById('serviceSelect');

    // Устанавливаем начальные значения для услуги
    if (serviceSelect) {
        updateServiceDetails(serviceSelect);
    }

    // Загружаем временные слоты
    loadTimeSlots();

    // Слушатели изменений
    const dateInput = document.getElementById('dateInput');
    const timeSelect = document.getElementById('timeSelect');

    if (dateInput) {
        dateInput.addEventListener('change', function () {
            loadTimeSlots(); // Перезагружаем слоты при изменении даты
            updateDateTime();
        });
    }

    if (timeSelect) {
        timeSelect.addEventListener('change', updateDateTime);
    }
}

// Экспортируем функции для использования в HTML
window.updateServiceDetails = updateServiceDetails;
window.loadTimeSlots = loadTimeSlots;
window.updateDateTime = updateDateTime;
window.initializeAppointmentForm = initializeAppointmentForm;
window.validateDateTime = validateDateTime;
window.loadTimeSettings = loadTimeSettings;
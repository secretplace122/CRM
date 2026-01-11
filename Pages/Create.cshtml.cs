using DG_CRM.Data;
using DG_CRM.Data.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace DG_CRM.Pages
{
    public class CreateModel : PageModel
    {
        private readonly AppDbContext _context;
        private readonly ILogger<CreateModel> _logger;

        public CreateModel(AppDbContext context, ILogger<CreateModel> logger)
        {
            _context = context;
            _logger = logger;
        }

        [BindProperty]
        public Appointment Appointment { get; set; } = new();

        public async Task<IActionResult> OnGetAsync()
        {
            // Устанавливаем время по умолчанию (через час, без секунд)
            var now = DateTime.Now.AddHours(1);
            Appointment.StartTime = new DateTime(now.Year, now.Month, now.Day, now.Hour, now.Minute, 0);
            _logger.LogInformation($"Default StartTime set to: {Appointment.StartTime}");

            // Загружаем услуги для выпадающего списка
            var services = await _context.Services
                .Where(s => s.IsActive)
                .OrderBy(s => s.Name)
                .ToListAsync();

            ViewData["Services"] = services;

            // Устанавливаем первую услугу по умолчанию если есть
            if (services.Any())
            {
                var firstService = services.First();
                Appointment.ServiceName = firstService.Name;
                Appointment.Price = firstService.Price;
                Appointment.DurationMinutes = firstService.DurationMinutes;
            }

            return Page();
        }

        public async Task<IActionResult> OnPostAsync()
        {
            _logger.LogInformation("=== POST Create Start ===");
            _logger.LogInformation($"FullName: {Appointment.FullName}");
            _logger.LogInformation($"Phone: {Appointment.Phone}");
            _logger.LogInformation($"Service: {Appointment.ServiceName}");
            _logger.LogInformation($"StartTime: {Appointment.StartTime}");
            _logger.LogInformation($"StartTime Kind: {Appointment.StartTime.Kind}");

            // Загружаем услуги для формы (если будет ошибка)
            var services = await _context.Services
                .Where(s => s.IsActive)
                .OrderBy(s => s.Name)
                .ToListAsync();

            ViewData["Services"] = services;

            // Простая валидация
            if (string.IsNullOrWhiteSpace(Appointment.FullName))
            {
                ModelState.AddModelError("Appointment.FullName", "ФИО обязательно");
            }

            if (string.IsNullOrWhiteSpace(Appointment.Phone))
            {
                ModelState.AddModelError("Appointment.Phone", "Телефон обязателен");
            }

            if (string.IsNullOrWhiteSpace(Appointment.ServiceName))
            {
                ModelState.AddModelError("Appointment.ServiceName", "Услуга обязательна");
            }

            // Усиленная валидация времени
            var now = DateTime.Now;

            // Проверяем, что дата не в прошлом
            if (Appointment.StartTime.Date < now.Date)
            {
                ModelState.AddModelError("Appointment.StartTime",
                    "Нельзя записать на прошедшую дату");
            }
            // Если сегодня, проверяем время
            else if (Appointment.StartTime.Date == now.Date)
            {
                if (Appointment.StartTime.TimeOfDay < now.TimeOfDay)
                {
                    ModelState.AddModelError("Appointment.StartTime",
                        $"Нельзя записать на прошедшее время. Сейчас {now:HH:mm}");
                }
                // Добавляем буфер в 15 минут
                else if (Appointment.StartTime.TimeOfDay < now.AddMinutes(15).TimeOfDay)
                {
                    ModelState.AddModelError("Appointment.StartTime",
                        $"Запись должна быть минимум на 15 минут позже текущего времени. Сейчас {now:HH:mm}");
                }
            }

            // Проверяем кратность времени из настроек
            var settings = await _context.Settings.FindAsync(1);
            if (settings != null)
            {
                if (Appointment.StartTime.Minute % settings.TimeSlotInterval != 0)
                {
                    ModelState.AddModelError("Appointment.StartTime",
                        $"Время должно быть кратно {settings.TimeSlotInterval} минутам. " +
                        $"Например: 10:00, 10:{settings.TimeSlotInterval:00}, 10:{settings.TimeSlotInterval * 2:00}, ...");
                }
            }

            // Проверяем что цена и длительность соответствуют выбранной услуге
            if (!string.IsNullOrEmpty(Appointment.ServiceName))
            {
                var selectedService = await _context.Services
                    .FirstOrDefaultAsync(s => s.Name == Appointment.ServiceName && s.IsActive);

                if (selectedService != null)
                {
                    // Автоматически подставляем цену и длительность из услуги
                    Appointment.Price = selectedService.Price;
                    Appointment.DurationMinutes = selectedService.DurationMinutes;
                    _logger.LogInformation($"Using service price: {selectedService.Price}, duration: {selectedService.DurationMinutes}");
                }
                else
                {
                    ModelState.AddModelError("Appointment.ServiceName", "Выбранная услуга не найдена или неактивна");
                }
            }

            if (!ModelState.IsValid)
            {
                _logger.LogWarning("ModelState invalid:");
                foreach (var error in ModelState)
                {
                    _logger.LogWarning($"  {error.Key}: {string.Join(", ", error.Value.Errors.Select(e => e.ErrorMessage))}");
                }
                return Page();
            }

            try
            {
                // Обеспечиваем, что время без секунд
                Appointment.StartTime = new DateTime(
                    Appointment.StartTime.Year,
                    Appointment.StartTime.Month,
                    Appointment.StartTime.Day,
                    Appointment.StartTime.Hour,
                    Appointment.StartTime.Minute,
                    0);

                Appointment.CreatedAt = DateTime.UtcNow;
                Appointment.Status = AppointmentStatus.Scheduled;

                _logger.LogInformation($"Saving appointment: {Appointment.FullName}, Time: {Appointment.StartTime}");

                _context.Appointments.Add(Appointment);
                await _context.SaveChangesAsync();

                _logger.LogInformation($"Appointment saved with ID: {Appointment.Id}");

                TempData["SuccessMessage"] = $"Запись для {Appointment.FullName} успешно создана!";
                return RedirectToPage("./Index");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating appointment");
                ModelState.AddModelError("", $"Ошибка при создании: {ex.Message}");
                return Page();
            }
        }
    }
}
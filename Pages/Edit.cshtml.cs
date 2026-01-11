using DG_CRM.Data;
using DG_CRM.Data.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace DG_CRM.Pages
{
    public class EditModel : PageModel
    {
        private readonly AppDbContext _context;
        private readonly ILogger<EditModel> _logger;

        public EditModel(AppDbContext context, ILogger<EditModel> logger)
        {
            _context = context;
            _logger = logger;
        }

        [BindProperty]
        public Appointment Appointment { get; set; } = default!;

        public async Task<IActionResult> OnGetAsync(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var appointment = await _context.Appointments.FindAsync(id);
            if (appointment == null)
            {
                return NotFound();
            }
            Appointment = appointment;

            // Загружаем услуги для выпадающего списка
            var services = await _context.Services
                .Where(s => s.IsActive)
                .OrderBy(s => s.Name)
                .ToListAsync();

            ViewData["Services"] = services;

            // Если услуга есть в записи, устанавливаем цену и длительность
            if (!string.IsNullOrEmpty(Appointment.ServiceName) && Appointment.Price == 0)
            {
                var service = services.FirstOrDefault(s => s.Name == Appointment.ServiceName);
                if (service != null)
                {
                    Appointment.Price = service.Price;
                    Appointment.DurationMinutes = service.DurationMinutes;
                }
            }

            return Page();
        }

        public async Task<IActionResult> OnPostAsync()
        {
            _logger.LogInformation("=== POST Edit Start ===");
            _logger.LogInformation($"Editing appointment ID: {Appointment.Id}");
            _logger.LogInformation($"FullName: {Appointment.FullName}");
            _logger.LogInformation($"Service: {Appointment.ServiceName}");
            _logger.LogInformation($"StartTime: {Appointment.StartTime}");

            // Загружаем услуги для формы (если будет ошибка)
            var services = await _context.Services
                .Where(s => s.IsActive)
                .OrderBy(s => s.Name)
                .ToListAsync();

            ViewData["Services"] = services;

            // Валидация
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

            // Для редактирования разрешаем прошедшее время, но проверяем корректность
            var now = DateTime.Now;

            // Если это запись в будущем, проверяем что она не слишком близко к текущему времени
            if (Appointment.StartTime > now && Appointment.StartTime.Date == now.Date)
            {
                var timeDiff = (Appointment.StartTime - now).TotalMinutes;
                if (timeDiff < 15)
                {
                    ModelState.AddModelError("Appointment.StartTime",
                        $"Новая запись должна быть минимум на 15 минут позже текущего времени. Сейчас {now:HH:mm}");
                }
            }

            // Проверяем кратность времени из настроек
            var settings = await _context.Settings.FindAsync(1);
            if (settings != null && Appointment.StartTime.Minute % settings.TimeSlotInterval != 0)
            {
                ModelState.AddModelError("Appointment.StartTime",
                    $"Время должно быть кратно {settings.TimeSlotInterval} минутам. " +
                    $"Например: 10:00, 10:{settings.TimeSlotInterval:00}, 10:{settings.TimeSlotInterval * 2:00}, ...");
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

                // Получаем существующую запись
                var existingAppointment = await _context.Appointments
                    .FirstOrDefaultAsync(a => a.Id == Appointment.Id);

                if (existingAppointment == null)
                {
                    return NotFound();
                }

                // Обновляем поля
                existingAppointment.FullName = Appointment.FullName;
                existingAppointment.Phone = Appointment.Phone;
                existingAppointment.Email = Appointment.Email;
                existingAppointment.ServiceName = Appointment.ServiceName;
                existingAppointment.Price = Appointment.Price;
                existingAppointment.DurationMinutes = Appointment.DurationMinutes;
                existingAppointment.StartTime = Appointment.StartTime;
                existingAppointment.Status = Appointment.Status;
                existingAppointment.Notes = Appointment.Notes;

                await _context.SaveChangesAsync();

                TempData["SuccessMessage"] = $"Запись для {Appointment.FullName} обновлена!";
                return RedirectToPage("./Index");
            }
            catch (DbUpdateConcurrencyException ex)
            {
                _logger.LogError(ex, "Concurrency error");
                if (!AppointmentExists(Appointment.Id))
                {
                    return NotFound();
                }
                else
                {
                    throw;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating appointment");
                ModelState.AddModelError("", $"Ошибка при обновлении: {ex.Message}");
                return Page();
            }
        }

        private bool AppointmentExists(int id)
        {
            return _context.Appointments.Any(e => e.Id == id);
        }
    }
}
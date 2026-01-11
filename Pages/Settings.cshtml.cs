using DG_CRM.Data;
using DG_CRM.Data.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace DG_CRM.Pages
{
    public class SettingsModel : PageModel
    {
        private readonly AppDbContext _context;
        private readonly ILogger<SettingsModel> _logger;

        public SettingsModel(AppDbContext context, ILogger<SettingsModel> logger)
        {
            _context = context;
            _logger = logger;
        }

        [BindProperty]
        public Settings SystemSettings { get; set; } = new();

        [BindProperty]
        public Service NewService { get; set; } = new();

        public List<Service> Services { get; set; } = new();

        public async Task<IActionResult> OnGetAsync()
        {
            // Загружаем настройки (всегда одна запись с Id=1)
            SystemSettings = await _context.Settings.FindAsync(1) ?? new Settings();

            // Загружаем услуги
            Services = await _context.Services
                .Where(s => s.IsActive)
                .OrderBy(s => s.Category)
                .ThenBy(s => s.Name)
                .ToListAsync();

            return Page();
        }

        public async Task<IActionResult> OnPostSaveSettingsAsync()
        {
            _logger.LogInformation("Saving settings...");
            _logger.LogInformation($"TimeSlotInterval: {SystemSettings.TimeSlotInterval}");
            _logger.LogInformation($"WorkDayStart: {SystemSettings.WorkDayStart}");
            _logger.LogInformation($"WorkDayEnd: {SystemSettings.WorkDayEnd}");
            _logger.LogInformation($"BreakBetweenSlots: {SystemSettings.BreakBetweenSlots}");

            // Загружаем текущие настройки для отображения при ошибке
            Services = await _context.Services.ToListAsync();

            if (!ModelState.IsValid)
            {
                _logger.LogWarning("ModelState invalid");
                return Page();
            }

            try
            {
                var existingSettings = await _context.Settings.FindAsync(1);
                if (existingSettings == null)
                {
                    // Создаем новую запись
                    SystemSettings.Id = 1;
                    SystemSettings.LastUpdated = DateTime.UtcNow;
                    _context.Settings.Add(SystemSettings);
                    _logger.LogInformation("Created new settings record");
                }
                else
                {
                    // Обновляем существующую запись - ВАЖНО: используем WorkDayEnd, а не WorkEnd
                    existingSettings.TimeSlotInterval = SystemSettings.TimeSlotInterval;
                    existingSettings.WorkDayStart = SystemSettings.WorkDayStart;
                    existingSettings.WorkDayEnd = SystemSettings.WorkDayEnd; // Исправлено
                    existingSettings.BreakBetweenSlots = SystemSettings.BreakBetweenSlots;
                    existingSettings.LastUpdated = DateTime.UtcNow;
                    _logger.LogInformation("Updated existing settings record");
                }

                await _context.SaveChangesAsync();
                _logger.LogInformation("Settings saved successfully");

                TempData["SuccessMessage"] = "Настройки сохранены!";
                return RedirectToPage();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error saving settings");
                ModelState.AddModelError("", $"Ошибка при сохранении: {ex.Message}");
                return Page();
            }
        }

        public async Task<IActionResult> OnPostAddServiceAsync()
        {
            if (!ModelState.IsValid)
            {
                SystemSettings = await _context.Settings.FindAsync(1) ?? new Settings();
                Services = await _context.Services.ToListAsync();
                return Page();
            }

            NewService.CreatedAt = DateTime.UtcNow;
            NewService.IsActive = true;

            _context.Services.Add(NewService);
            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = $"Услуга '{NewService.Name}' добавлена!";
            NewService = new Service();

            return RedirectToPage();
        }

        public async Task<IActionResult> OnPostDeleteServiceAsync(int id)
        {
            var service = await _context.Services.FindAsync(id);
            if (service != null)
            {
                // Мягкое удаление - деактивация
                service.IsActive = false;
                service.UpdatedAt = DateTime.UtcNow;
                await _context.SaveChangesAsync();

                TempData["SuccessMessage"] = $"Услуга '{service.Name}' удалена!";
            }

            return RedirectToPage();
        }

        public async Task<IActionResult> OnPostUpdateServiceAsync(int id, string name, decimal price, int duration)
        {
            var service = await _context.Services.FindAsync(id);
            if (service != null)
            {
                service.Name = name;
                service.Price = price;
                service.DurationMinutes = duration;
                service.UpdatedAt = DateTime.UtcNow;

                await _context.SaveChangesAsync();

                TempData["SuccessMessage"] = $"Услуга '{service.Name}' обновлена!";
            }

            return RedirectToPage();
        }
    }
}
using DG_CRM.Data;
using DG_CRM.Data.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace DG_CRM.Pages
{
    public class IndexModel : PageModel
    {
        private readonly AppDbContext _context;

        public IndexModel(AppDbContext context)
        {
            _context = context;
        }

        public List<Appointment> Appointments { get; set; } = new();

        [BindProperty(SupportsGet = true)]
        public string SearchString { get; set; } = string.Empty;

        // Добавляем свойства для настроек
        public int TimeSlotInterval { get; set; } = 20;
        public string WorkDayStart { get; set; } = "09:00";
        public string WorkDayEnd { get; set; } = "21:00";

        public async Task OnGetAsync()
        {
            // Загружаем настройки
            var settings = await _context.Settings.FindAsync(1);
            if (settings != null)
            {
                TimeSlotInterval = settings.TimeSlotInterval;
                WorkDayStart = settings.WorkDayStart.ToString(@"hh\:mm");
                WorkDayEnd = settings.WorkDayEnd.ToString(@"hh\:mm");
            }

            // Загружаем записи
            IQueryable<Appointment> query = _context.Appointments;

            if (!string.IsNullOrEmpty(SearchString))
            {
                var searchLower = SearchString.ToLower().Trim();
                query = query.Where(a =>
                    a.FullName.ToLower().Contains(searchLower) ||
                    a.Phone.Contains(SearchString) ||
                    (!string.IsNullOrEmpty(a.Email) && a.Email.ToLower().Contains(searchLower)) ||
                    a.ServiceName.ToLower().Contains(searchLower));
            }

            Appointments = await query
                .OrderByDescending(a => a.StartTime)
                .ToListAsync();
        }

        public async Task<IActionResult> OnPostDeleteAsync(int id)
        {
            var appointment = await _context.Appointments.FindAsync(id);
            if (appointment != null)
            {
                _context.Appointments.Remove(appointment);
                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = "Запись удалена!";
            }

            return RedirectToPage();
        }

        public async Task<IActionResult> OnPostAddTestServiceAsync()
        {
            // Добавляем тестовые услуги если их нет
            if (!_context.Services.Any())
            {
                _context.Services.Add(new Service
                {
                    Name = "Стрижка",
                    Price = 1500,
                    DurationMinutes = 60,
                    IsActive = true,
                    CreatedAt = DateTime.UtcNow
                });

                _context.Services.Add(new Service
                {
                    Name = "Маникюр",
                    Price = 2000,
                    DurationMinutes = 90,
                    IsActive = true,
                    CreatedAt = DateTime.UtcNow
                });

                await _context.SaveChangesAsync();

                TempData["SuccessMessage"] = "Тестовые услуги добавлены!";
            }

            return RedirectToPage();
        }
    }
}
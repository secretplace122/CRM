using DG_CRM.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace DG_CRM.Pages
{
    public class TimeSettingsModel : PageModel
    {
        private readonly AppDbContext _context;

        public TimeSettingsModel(AppDbContext context)
        {
            _context = context;
        }

        public IActionResult OnGetTimeSettings()
        {
            var settings = _context.Settings.Find(1);

            return new JsonResult(new
            {
                interval = settings?.TimeSlotInterval ?? 20,
                workStart = settings?.WorkDayStart.ToString(@"hh\:mm") ?? "09:00",
                workEnd = settings?.WorkDayEnd.ToString(@"hh\:mm") ?? "21:00"
            });
        }
    }
}
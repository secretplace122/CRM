using System.ComponentModel.DataAnnotations;

namespace DG_CRM.Data.Models
{
    public class Settings
    {
        public int Id { get; set; }

        [Display(Name = "Кратность времени (минут)")]
        [Range(5, 60, ErrorMessage = "Кратность должна быть от 5 до 60 минут")]
        public int TimeSlotInterval { get; set; } = 20; // По умолчанию 20 минут

        [Display(Name = "Начало рабочего дня")]
        [DataType(DataType.Time)]
        public TimeSpan WorkDayStart { get; set; } = new TimeSpan(9, 0, 0); // 09:00

        [Display(Name = "Конец рабочего дня")]
        [DataType(DataType.Time)]
        public TimeSpan WorkDayEnd { get; set; } = new TimeSpan(21, 0, 0); // 21:00

        [Display(Name = "Перерыв между записями (минут)")]
        [Range(0, 60, ErrorMessage = "Перерыв должен быть от 0 до 60 минут")]
        public int BreakBetweenSlots { get; set; } = 20;

        public DateTime LastUpdated { get; set; } = DateTime.UtcNow;
    }
}
using System.ComponentModel.DataAnnotations;

namespace DG_CRM.Data.Models
{
    public class Appointment
    {
        public int Id { get; set; }

        // [Required(ErrorMessage = "ФИО обязательно")]
        // [Display(Name = "ФИО клиента")]
        public string FullName { get; set; } = string.Empty;

        // [Required(ErrorMessage = "Телефон обязателен")]
        // [Phone(ErrorMessage = "Неверный формат телефона")]
        // [Display(Name = "Телефон")]
        public string Phone { get; set; } = string.Empty;

        // [EmailAddress(ErrorMessage = "Неверный формат email")]
        // [Display(Name = "Email")]
        public string? Email { get; set; }

        // [Display(Name = "Заметки")]
        public string? Notes { get; set; }

        // [Required(ErrorMessage = "Услуга обязательна")]
        // [Display(Name = "Услуга")]
        public string ServiceName { get; set; } = string.Empty;

        // [Required(ErrorMessage = "Длительность обязательна")]
        // [Display(Name = "Длительность (минут)")]
        public int DurationMinutes { get; set; } = 60;

        // [Display(Name = "Цена")]
        // [Range(0, 1000000, ErrorMessage = "Некорректная цена")]
        public decimal Price { get; set; }

        // [Required(ErrorMessage = "Дата и время обязательны")]
        // [Display(Name = "Начало")]
        public DateTime StartTime { get; set; }

        // [Display(Name = "Статус")]
        public AppointmentStatus Status { get; set; } = AppointmentStatus.Scheduled;

        // [Display(Name = "Создано")]
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        // [Display(Name = "Время окончания")]
        public DateTime EndTime => StartTime.AddMinutes(DurationMinutes);
    }

    public enum AppointmentStatus
    {
        [Display(Name = "Запланировано")]
        Scheduled,

        [Display(Name = "Подтверждено")]
        Confirmed,

        [Display(Name = "В процессе")]
        InProgress,

        [Display(Name = "Завершено")]
        Completed,

        [Display(Name = "Отменено")]
        Cancelled
    }

}
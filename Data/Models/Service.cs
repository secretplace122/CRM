using System.ComponentModel.DataAnnotations;

namespace DG_CRM.Data.Models
{
    public class Service
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "Название услуги обязательно")]
        [Display(Name = "Название услуги")]
        [MaxLength(100)]
        public string Name { get; set; } = string.Empty;

        [Required(ErrorMessage = "Цена обязательна")]
        [Display(Name = "Цена (₽)")]
        [Range(0, 100000, ErrorMessage = "Цена должна быть от 0 до 100000")]
        public decimal Price { get; set; }

        [Required(ErrorMessage = "Длительность обязательна")]
        [Display(Name = "Длительность (минут)")]
        [Range(1, 480, ErrorMessage = "Длительность должна быть от 1 до 480 минут")]
        public int DurationMinutes { get; set; } = 60;

        [Display(Name = "Описание")]
        [MaxLength(500)]
        public string? Description { get; set; }

        [Display(Name = "Активна")]
        public bool IsActive { get; set; } = true;

        [Display(Name = "Категория")]
        [MaxLength(50)]
        public string? Category { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? UpdatedAt { get; set; }
    }
}
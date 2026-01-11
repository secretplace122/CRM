using DG_CRM.Data.Models;
using Microsoft.EntityFrameworkCore;

namespace DG_CRM.Data
{
    public class AppDbContext : DbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> options)
            : base(options)
        {
        }

        public DbSet<Appointment> Appointments { get; set; }
        public DbSet<Service> Services { get; set; }
        public DbSet<Settings> Settings { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // Appointment
            modelBuilder.Entity<Appointment>()
                .Property(a => a.Price)
                .HasPrecision(18, 2);

            // Service
            modelBuilder.Entity<Service>()
                .Property(s => s.Price)
                .HasPrecision(18, 2);

            // Settings - только одна запись
            modelBuilder.Entity<Settings>()
                .HasData(new Settings
                {
                    Id = 1,
                    TimeSlotInterval = 20,
                    WorkDayStart = new TimeSpan(9, 0, 0),
                    WorkDayEnd = new TimeSpan(21, 0, 0),
                    BreakBetweenSlots = 20,
                    LastUpdated = DateTime.UtcNow
                });
        }
    }
}
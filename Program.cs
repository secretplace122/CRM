using DG_CRM.Data;
using DG_CRM.Data.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// Добавление сервисов в контейнер
builder.Services.AddRazorPages();

// Настройка базы данных SQLite
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlite("Data Source=dgcrm.db"));

var app = builder.Build();

// Настройка конвейера HTTP запросов
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseRouting();
app.UseAuthorization();
app.MapRazorPages();

// ПРОСТОЕ СОЗДАНИЕ БАЗЫ ДАННЫХ
using (var scope = app.Services.CreateScope())
{
    var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();

    // 1. Создаем таблицы если их нет
    dbContext.Database.EnsureCreated();
    Console.WriteLine("Таблицы созданы (если не существовали)");

    // 2. Добавляем настройки если таблица пустая
    if (!dbContext.Settings.Any())
    {
        dbContext.Settings.Add(new Settings { Id = 1, TimeSlotInterval = 20 });
        Console.WriteLine("Добавлены настройки");
    }

    // 3. Добавляем тестовую услугу
    if (!dbContext.Services.Any())
    {
        dbContext.Services.Add(new Service
        {
            Name = "Тестовая услуга",
            Price = 1000,
            DurationMinutes = 60,
            IsActive = true
        });
        Console.WriteLine("Добавлена тестовая услуга");
    }

    // 4. Добавляем тестовую запись
    if (!dbContext.Appointments.Any())
    {
        dbContext.Appointments.Add(new Appointment
        {
            FullName = "Тестовый клиент",
            Phone = "+79990001122",
            ServiceName = "Тестовая услуга",
            StartTime = DateTime.Today.AddHours(10)
        });
        Console.WriteLine("Добавлена тестовая запись");
    }

    await dbContext.SaveChangesAsync();
    Console.WriteLine("База данных инициализирована!");
}
app.MapRazorPages();
app.MapGet("/TimeSettings", (HttpContext context, [FromServices] AppDbContext dbContext) =>
{
    var settings = dbContext.Settings.Find(1);
    return Results.Json(new
    {
        interval = settings?.TimeSlotInterval ?? 20,
        workStart = settings?.WorkDayStart.ToString(@"hh\:mm") ?? "09:00",
        workEnd = settings?.WorkDayEnd.ToString(@"hh\:mm") ?? "21:00"
    });
});
app.Run();
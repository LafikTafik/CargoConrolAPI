using Bogus;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using CCAPI.Models;

namespace CCAPI.Services;

public class DataSeeder : IHostedService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<DataSeeder> _logger;

    public DataSeeder(IServiceProvider serviceProvider, ILogger<DataSeeder> logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        using var scope = _serviceProvider.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        // 🔹 Проверяем, есть ли уже данные — если есть, ничего не делаем
        if (await context.Users.AnyAsync(cancellationToken))
        {
            _logger.LogInformation("✅ Тестовые данные уже существуют — пропускаем генерацию.");
            return;
        }

        _logger.LogInformation("⏳ Начинаем генерацию тестовых данных...");
        var ruFaker = new Faker("ru"); // ← Русская локализация!
        // 🎲 Генераторы

        // === 1. Роли ===
        var roles = new[]
        {
    new Role { Name = "Admin", Description = "Администратор системы" },
    new Role { Name = "Moderator", Description = "Модератор — может управлять заказами" },
    new Role { Name = "User", Description = "Обычный пользователь" },
    new Role { Name = "Driver", Description = "Водитель транспорта" }
};

        await context.Roles.AddRangeAsync(roles, cancellationToken);
        await context.SaveChangesAsync(cancellationToken);

        var roleIds = roles.ToDictionary(r => r.Name, r => r.Id);

        // === 2. Клиенты ===
        var clientFaker = new Faker<Client>("ru")
             .RuleFor(c => c.Name, f => f.Name.FirstName())
            .RuleFor(c => c.Surname, f => f.Name.LastName())
            .RuleFor(c => c.Phone, f => $"8{f.Random.Replace("##########")}")
            .RuleFor(c => c.Email, (f, c) => f.Internet.Email(c.Name, c.Surname))
            .RuleFor(c => c.Adress, f => f.Address.FullAddress());

        var clients = clientFaker.Generate(1000);
        await context.Clients.AddRangeAsync(clients, cancellationToken);
        await context.SaveChangesAsync(cancellationToken);

        var clientIds = clients.Select(c => c.ID).ToArray();

        // === 3. Водители ===
        var driverFaker = new Faker<Driver>("ru")
            .RuleFor(d => d.FirstName, f => f.Name.FirstName())
            .RuleFor(d => d.LastName, f => f.Name.LastName())
            .RuleFor(d => d.LicenseNumber, f => f.Random.Replace("??###??"))
            .RuleFor(d => d.PhoneNumber, f => $"8{f.Random.Replace("##########")}");

        var drivers = driverFaker.Generate(500);
        await context.Drivers.AddRangeAsync(drivers, cancellationToken);
        await context.SaveChangesAsync(cancellationToken);

        var driverIds = drivers.Select(d => d.ID).ToArray();

        // === 4. Транспортные компании ===
        var companyFaker = new Faker<TransportationCompany>("ru")
            .RuleFor(tc => tc.Name, f => f.Company.CompanyName())
            .RuleFor(tc => tc.Address, f => f.Address.FullAddress())
            .RuleFor(tc => tc.PhoneNumber, f => $"8{f.Random.Replace("##########")}");

        var companies = companyFaker.Generate(1000);
        await context.TransportationCompany.AddRangeAsync(companies, cancellationToken);
        await context.SaveChangesAsync(cancellationToken);

        var companyIds = companies.Select(c => c.ID).ToArray();

        // === 5. Транспортные средства ===
        var vehicleFaker = new Faker<Vehicle>("ru")
            .RuleFor(v => v.Type, f => f.PickRandom(new[] { "Грузовик", "Фура", "Фургон", "Самосвал", "Тягач" }))
            .RuleFor(v => v.Capacity, f => $"{f.Random.Int(1, 50)} тонн")
            .RuleFor(v => v.VehicleNum, f => $"{f.Random.Replace("А")}{f.Random.Number(100, 999)}{f.Random.Replace("АА")} {f.Random.Number(1, 999)}")
            .RuleFor(v => v.TransportationCompanyId, f => f.PickRandom(companyIds))
            .RuleFor(v => v.DriverId, f => f.PickRandom(driverIds));

        var vehicles = vehicleFaker.Generate(1000);
        await context.Vehicle.AddRangeAsync(vehicles, cancellationToken);
        await context.SaveChangesAsync(cancellationToken);

        var vehicleIds = vehicles.Select(v => v.ID).ToArray();

        // === 6. Грузы ===
        var cargoFaker = new Faker<Cargos>()
            .RuleFor(c => c.Weight, f => $"{f.Random.Decimal(0.1m, 1000m)} кг")
            .RuleFor(c => c.Dimensions, f => $"{f.Random.Int(10, 300)}x{f.Random.Int(10, 200)}x{f.Random.Int(10, 150)} см")
            .RuleFor(c => c.Descriptions, f => f.Commerce.ProductDescription())
            .RuleFor(c => c.IsDeleted, false);

        var cargos = cargoFaker.Generate(1000);
        await context.Cargo.AddRangeAsync(cargos, cancellationToken);
        await context.SaveChangesAsync(cancellationToken);

        var cargoIds = cargos.Select(c => c.ID).ToArray();

        // === 7. Транспортировки ===
        var transportationFaker = new Faker<Transportation>()
            .RuleFor(t => t.StartPoint, f => f.Address.City())
            .RuleFor(t => t.EndPoint, f => f.Address.City())
            .RuleFor(t => t.CargoID, 0) // временно
            .RuleFor(t => t.VehicleId, f => f.PickRandom(vehicleIds))
            .RuleFor(t => t.IsDeleted, false);

        var transportations = transportationFaker.Generate(500);
        await context.Transportations.AddRangeAsync(transportations, cancellationToken);
        await context.SaveChangesAsync(cancellationToken); // ← Сохраняем Transportation

        // 🔥 Добавь эту строку — сбросить кэш EF Core
        context.ChangeTracker.Clear();

        // Получаем реальные ID
        var transportationIds = transportations.Select(t => t.ID).ToArray();

        // Обновляем CargoID
        for (int i = 0; i < transportations.Count; i++)
        {
            transportations[i].CargoID = cargoIds[i % cargoIds.Length];
        }
        await context.SaveChangesAsync(cancellationToken); // ← Сохраняем обновлённые Transportation

        // 🔥 Снова сбросить кэш
        context.ChangeTracker.Clear();

        // === 8. Заказы ===
        var orderFaker = new Faker<Orders>()
            .RuleFor(o => o.TransId, f => f.PickRandom(transportationIds)) // ← Теперь ID реальные
            .RuleFor(o => o.IDClient, f => f.PickRandom(clientIds))
            .RuleFor(o => o.Date, f => f.Date.Recent(30))
            .RuleFor(o => o.Status, f => f.PickRandom(new[] { "Pending", "Confirmed", "Shipped", "Delivered", "Cancelled" }))
            .RuleFor(o => o.Price, f => f.Random.Decimal(100m, 10000m))
            .RuleFor(o => o.IsDeleted, false);

        var orders = orderFaker.Generate(1000);
        await context.Order.AddRangeAsync(orders, cancellationToken);
        await context.SaveChangesAsync(cancellationToken); // ← Теперь сохранится без ошибки
        var orderIds = orders.Select(o => o.ID).ToArray();

        // === 9. Связь Cargo ↔ Order (через CargoOrders) ===
        var cargoOrdersList = new List<CargoOrders>();
        for (int i = 0; i < 1000; i++)
        {
            cargoOrdersList.Add(new CargoOrders
            {
                CargoID = cargoIds[i % cargoIds.Length],
                OrderID = orderIds[i % orderIds.Length] // ← Заполняем OrderID реальным ID
            });
        }
        await context.CargoOrders.AddRangeAsync(cargoOrdersList, cancellationToken);
        await context.SaveChangesAsync(cancellationToken);

        // === 10. Пользователи ===
        var userFaker = new Faker<User>()
            .RuleFor(u => u.Email, f => f.Internet.Email())
            .RuleFor(u => u.PasswordHash, f => BCrypt.Net.BCrypt.HashPassword("password123"))
            .RuleFor(u => u.RoleId, f => f.PickRandom(roleIds.Values.ToArray()))
            .RuleFor(u => u.ClientID, (f, u) => u.RoleId == roleIds["User"] ? f.PickRandom(clientIds) : (int?)null)
            .RuleFor(u => u.DriverID, (f, u) => u.RoleId == roleIds["Driver"] ? f.PickRandom(driverIds) : (int?)null)
            .RuleFor(u => u.IsDeleted, false);

        var users = userFaker.Generate(1000);
        await context.Users.AddRangeAsync(users, cancellationToken);
        await context.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("✅ Успешно сгенерировано ~500 записей в каждой таблице!");
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}
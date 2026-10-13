using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using AquaMonitor.API.Data;

namespace AquaMonitor.Tests;

public class CustomWebApplicationFactory : WebApplicationFactory<Program>
{
    // Cada classe de teste (fixture) recebe um banco InMemory próprio.
    // O xUnit executa as classes em paralelo; com um nome fixo, duas fixtures
    // tentavam semear o mesmo Sensor Id = 1 ao mesmo tempo ("same key has already been added").
    private readonly string _dbName = $"AquaMonitorTestDb_{Guid.NewGuid()}";

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.ConfigureServices(services =>
        {
            // Remove o DbContext real (Oracle)
            var descriptor = services.SingleOrDefault(
                d => d.ServiceType == typeof(DbContextOptions<AppDbContext>));

            if (descriptor != null)
                services.Remove(descriptor);

            // Adiciona InMemory para testes
            services.AddDbContext<AppDbContext>(options =>
                options.UseInMemoryDatabase(_dbName));

            // Garante que o banco é criado
            var sp = services.BuildServiceProvider();
            using var scope = sp.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            db.Database.EnsureCreated();

            // Seed com dados de teste
            SeedTestData(db);
        });
    }

    private static void SeedTestData(AppDbContext db)
    {
        if (db.Sensores.Any()) return;

        db.Sensores.Add(new AquaMonitor.API.Models.Sensor
        {
            Id = 1,
            Nome = "Sensor Teste",
            Localizacao = "Lab 01",
            Tipo = "Comercial",
            LimiteConsumo = 500,
            Ativo = true,
            CriadoEm = DateTime.UtcNow
        });

        db.Usuarios.Add(new AquaMonitor.API.Models.Usuario
        {
            Id = 1,
            Nome = "Admin Teste",
            Email = "admin@teste.com",
            SenhaHash = BCrypt.Net.BCrypt.HashPassword("123456"),
            Perfil = "Admin",
            CriadoEm = DateTime.UtcNow
        });

        db.SaveChanges();
    }
}

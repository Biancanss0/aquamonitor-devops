using Microsoft.EntityFrameworkCore;
using AquaMonitor.API.Models;

namespace AquaMonitor.API.Data;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    public DbSet<Sensor> Sensores { get; set; }
    public DbSet<Leitura> Leituras { get; set; }
    public DbSet<Alerta> Alertas { get; set; }
    public DbSet<Usuario> Usuarios { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Sensor
        modelBuilder.Entity<Sensor>(entity =>
        {
            entity.HasIndex(e => e.Nome);
            entity.Property(e => e.Ativo).HasDefaultValue(true);
            entity.Property(e => e.CriadoEm).HasDefaultValueSql("SYSDATE");
        });

        // Leitura
        modelBuilder.Entity<Leitura>(entity =>
        {
            entity.HasIndex(e => e.SensorId);
            entity.HasIndex(e => e.RegistradoEm);
            entity.Property(e => e.RegistradoEm).HasDefaultValueSql("SYSDATE");
            entity.Property(e => e.VazamentoDetectado).HasDefaultValue(false);

            entity.HasOne(l => l.Sensor)
                  .WithMany(s => s.Leituras)
                  .HasForeignKey(l => l.SensorId)
                  .OnDelete(DeleteBehavior.Cascade);
        });

        // Alerta
        modelBuilder.Entity<Alerta>(entity =>
        {
            entity.HasIndex(e => e.SensorId);
            entity.HasIndex(e => e.Resolvido);
            entity.Property(e => e.CriadoEm).HasDefaultValueSql("SYSDATE");
            entity.Property(e => e.Resolvido).HasDefaultValue(false);

            entity.HasOne(a => a.Sensor)
                  .WithMany(s => s.Alertas)
                  .HasForeignKey(a => a.SensorId)
                  .OnDelete(DeleteBehavior.Cascade);
        });

        // Usuario
        modelBuilder.Entity<Usuario>(entity =>
        {
            entity.HasIndex(e => e.Email).IsUnique();
            entity.Property(e => e.CriadoEm).HasDefaultValueSql("SYSDATE");
        });
    }
}

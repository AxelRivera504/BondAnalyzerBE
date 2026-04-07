using BondAnalyzer.API.Models;
using Microsoft.EntityFrameworkCore;

namespace BondAnalyzer.API.Data;

public class BondDbContext : DbContext
{
    public BondDbContext(DbContextOptions<BondDbContext> options) : base(options) { }

    public DbSet<Bono> Bonos { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<Bono>(entity =>
        {
            entity.ToTable("Bonos");

            entity.HasKey(e => e.Id);

            entity.Property(e => e.Id)
                  .ValueGeneratedOnAdd();

            entity.Property(e => e.Nombre)
                  .IsRequired()
                  .HasMaxLength(200);

            entity.Property(e => e.ValorNominal)
                  .HasColumnType("DECIMAL(18,2)")
                  .IsRequired();

            entity.Property(e => e.TasaCupon)
                  .HasColumnType("DECIMAL(10,6)")
                  .IsRequired();

            entity.Property(e => e.RentabilidadExigida)
                  .HasColumnType("DECIMAL(10,6)")
                  .IsRequired();

            entity.Property(e => e.PrecioCalculado)
                  .HasColumnType("DECIMAL(18,2)");

            entity.Property(e => e.TirOriginal)
                  .HasColumnType("DECIMAL(10,6)");

            entity.Property(e => e.TirComprador)
                  .HasColumnType("DECIMAL(10,6)");

            entity.Property(e => e.DuracionMacaulay)
                  .HasColumnType("DECIMAL(10,4)");

            entity.Property(e => e.DuracionModificada)
                  .HasColumnType("DECIMAL(10,4)");

            entity.Property(e => e.GspAbsoluto)
                  .HasColumnType("DECIMAL(18,2)");

            entity.Property(e => e.GspRelativo)
                  .HasColumnType("DECIMAL(10,6)");

            entity.Property(e => e.FechaCreacion)
                  .HasColumnType("DATETIME2")
                  .HasDefaultValueSql("GETUTCDATE()");
        });
    }
}

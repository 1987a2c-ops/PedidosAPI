using Microsoft.EntityFrameworkCore;
using PedidosAPI.Domain.Entities;

namespace PedidosAPI.Infrastructure.Data;

public class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
{
    public DbSet<PedidoCabecera> PedidosCabecera => Set<PedidoCabecera>();
    public DbSet<PedidoDetalle> PedidosDetalle => Set<PedidoDetalle>();
    public DbSet<LogAuditoria> LogsAuditoria => Set<LogAuditoria>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // PedidoCabecera
        modelBuilder.Entity<PedidoCabecera>(e =>
        {
            e.ToTable("PedidoCabecera");
            e.HasKey(x => x.Id);
            e.Property(x => x.Id).UseIdentityColumn();
            e.Property(x => x.ClienteId).IsRequired();
            e.Property(x => x.Fecha).IsRequired().HasDefaultValueSql("GETUTCDATE()");
            e.Property(x => x.Total).HasColumnType("decimal(18,2)").IsRequired();
            e.Property(x => x.Usuario).HasMaxLength(100).IsRequired();

            e.HasMany(x => x.Detalles)
             .WithOne(d => d.Pedido)
             .HasForeignKey(d => d.PedidoId)
             .OnDelete(DeleteBehavior.Cascade);
        });

        // PedidoDetalle
        modelBuilder.Entity<PedidoDetalle>(e =>
        {
            e.ToTable("PedidoDetalle");
            e.HasKey(x => x.Id);
            e.Property(x => x.Id).UseIdentityColumn();
            e.Property(x => x.ProductoId).IsRequired();
            e.Property(x => x.Cantidad).IsRequired();
            e.Property(x => x.Precio).HasColumnType("decimal(18,2)").IsRequired();
            e.Ignore(x => x.Subtotal); // propiedad calculada, no persistida
        });

        // LogAuditoria
        modelBuilder.Entity<LogAuditoria>(e =>
        {
            e.ToTable("LogAuditoria");
            e.HasKey(x => x.Id);
            e.Property(x => x.Id).UseIdentityColumn();
            e.Property(x => x.Fecha).IsRequired().HasDefaultValueSql("GETUTCDATE()");
            e.Property(x => x.Evento).HasMaxLength(100).IsRequired();
            e.Property(x => x.Descripcion).HasMaxLength(500).IsRequired();
            e.Property(x => x.Usuario).HasMaxLength(100);
            e.Property(x => x.Nivel).HasMaxLength(10).IsRequired();
        });
    }
}
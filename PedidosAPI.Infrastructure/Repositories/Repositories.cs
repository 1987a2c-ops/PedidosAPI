using PedidosAPI.Domain.Entities;
using PedidosAPI.Domain.Interfaces;
using PedidosAPI.Infrastructure.Data;

namespace PedidosAPI.Infrastructure.Repositories;

public sealed class PedidoRepository(AppDbContext context) : IPedidoRepository
{
    public async Task<PedidoCabecera> CrearAsync(PedidoCabecera pedido, CancellationToken ct = default)
    {
        await context.PedidosCabecera.AddAsync(pedido, ct);
        // SaveChanges lo ejecuta UnitOfWork.CommitAsync
        return pedido;
    }
}

public sealed class AuditoriaRepository(AppDbContext context) : IAuditoriaRepository
{
    public async Task RegistrarAsync(string evento, string descripcion,
        string? usuario = null, string nivel = "INFO", CancellationToken ct = default)
    {
        var log = new LogAuditoria
        {
            Fecha = DateTime.UtcNow,
            Evento = evento,
            Descripcion = descripcion,
            Usuario = usuario,
            Nivel = nivel
        };

        await context.LogsAuditoria.AddAsync(log, ct);
        // También persiste en el mismo commit de la transacción
    }
}
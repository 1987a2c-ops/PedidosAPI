using PedidosAPI.Domain.Entities;

namespace PedidosAPI.Domain.Interfaces;

public interface IPedidoRepository
{
    Task<PedidoCabecera> CrearAsync(PedidoCabecera pedido, CancellationToken ct = default);
}

public interface IAuditoriaRepository
{
    Task RegistrarAsync(string evento, string descripcion, string? usuario = null,
        string nivel = "INFO", CancellationToken ct = default);
}
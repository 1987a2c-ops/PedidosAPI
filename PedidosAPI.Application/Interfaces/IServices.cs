using PedidosAPI.Application.DTOs;

namespace PedidosAPI.Application.Interfaces;

public interface IPedidoService
{
    Task<CrearPedidoResponse> RegistrarPedidoAsync(CrearPedidoRequest request, CancellationToken ct = default);
    Task<ListaPedidosResponse> ObtenerTodosAsync(CancellationToken ct = default);
}

public interface IValidacionClienteService
{
    Task<bool> ValidarClienteAsync(int clienteId, CancellationToken ct = default);
}

public interface IUnitOfWork : IAsyncDisposable
{
    Task BeginTransactionAsync(CancellationToken ct = default);
    Task CommitAsync(CancellationToken ct = default);
    Task RollbackAsync(CancellationToken ct = default);
}
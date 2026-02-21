using PedidosAPI.Application.DTOs;
using PedidosAPI.Application.Interfaces;

namespace PedidosAPI.API.Endpoints;

public static class PedidosEndpoints
{
    public static IEndpointRouteBuilder MapPedidosEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/pedidos")
            .WithTags("Pedidos")
            .WithOpenApi();

        // POST /api/pedidos — Registrar nuevo pedido
        group.MapPost("/", RegistrarPedido)
            .WithName("RegistrarPedido")
            .WithSummary("Registrar un nuevo pedido")
            .WithDescription(
                "Registra un pedido con sus ítems. Valida el cliente con un servicio externo " +
                "y persiste todo dentro de una transacción. Incluye logs de auditoría.")
            .Produces<CrearPedidoResponse>(StatusCodes.Status201Created)
            .Produces<object>(StatusCodes.Status400BadRequest)
            .Produces<object>(StatusCodes.Status422UnprocessableEntity)
            .Produces<object>(StatusCodes.Status503ServiceUnavailable);

        // GET /api/pedidos — Obtener todos los pedidos
        group.MapGet("/", ObtenerTodos)
            .WithName("ObtenerTodosPedidos")
            .WithSummary("Obtener todos los pedidos registrados")
            .WithDescription(
                "Retorna la lista completa de pedidos con su cabecera y total de ítems, " +
                "ordenados del más reciente al más antiguo.")
            .Produces<ListaPedidosResponse>(StatusCodes.Status200OK);

        return app;
    }

    //  Handlers 

    private static async Task<IResult> RegistrarPedido(
        CrearPedidoRequest request,
        IPedidoService pedidoService,
        ILogger<Program> logger,
        CancellationToken ct)
    {
        logger.LogInformation("POST /api/pedidos → ClienteId={ClienteId}", request.ClienteId);

        var resultado = await pedidoService.RegistrarPedidoAsync(request, ct);

        return Results.Created($"/api/pedidos/{resultado.PedidoId}", resultado);
    }

    private static async Task<IResult> ObtenerTodos(
        IPedidoService pedidoService,
        ILogger<Program> logger,
        CancellationToken ct)
    {
        logger.LogInformation("GET /api/pedidos");

        var resultado = await pedidoService.ObtenerTodosAsync(ct);

        return Results.Ok(resultado);
    }
}
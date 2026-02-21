namespace PedidosAPI.Application.DTOs;

//  Request 

public record CrearPedidoRequest(
    int ClienteId,
    string Usuario,
    IEnumerable<ItemPedidoDto> Items
);

public record ItemPedidoDto(
    int ProductoId,
    int Cantidad,
    decimal Precio
);

//  Response 

public record CrearPedidoResponse(
    int PedidoId,
    int ClienteId,
    string Usuario,
    DateTime Fecha,
    decimal Total,
    IEnumerable<ItemPedidoResponseDto> Items
);

public record ItemPedidoResponseDto(
    int ProductoId,
    int Cantidad,
    decimal Precio,
    decimal Subtotal
);
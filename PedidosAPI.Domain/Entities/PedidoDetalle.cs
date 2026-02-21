namespace PedidosAPI.Domain.Entities;

public class PedidoDetalle
{
    public int Id { get; set; }
    public int PedidoId { get; set; }
    public int ProductoId { get; set; }
    public int Cantidad { get; set; }
    public decimal Precio { get; set; }
    public decimal Subtotal => Cantidad * Precio;

    public PedidoCabecera? Pedido { get; set; }
}
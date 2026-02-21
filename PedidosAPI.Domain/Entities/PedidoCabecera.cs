namespace PedidosAPI.Domain.Entities;

public class PedidoCabecera
{
    public int Id { get; set; }
    public int ClienteId { get; set; }
    public DateTime Fecha { get; set; } = DateTime.UtcNow;
    public decimal Total { get; set; }
    public string Usuario { get; set; } = string.Empty;
    public ICollection<PedidoDetalle> Detalles { get; set; } = new List<PedidoDetalle>();
}
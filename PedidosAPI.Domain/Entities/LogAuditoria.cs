namespace PedidosAPI.Domain.Entities;

public class LogAuditoria
{
    public int Id { get; set; }
    public DateTime Fecha { get; set; } = DateTime.UtcNow;
    public string Evento { get; set; } = string.Empty;
    public string Descripcion { get; set; } = string.Empty;
    public string? Usuario { get; set; }
    public string Nivel { get; set; } = "INFO"; // INFO | WARNING | ERROR
}
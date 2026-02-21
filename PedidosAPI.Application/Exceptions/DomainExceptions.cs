namespace PedidosAPI.Application.Exceptions;

public class PedidoInvalidoException(string mensaje) : Exception(mensaje);

public class ClienteNoValidoException(int clienteId)
    : Exception($"El cliente con Id {clienteId} no superó la validación externa.")
{
    public int ClienteId { get; } = clienteId;
}

public class ServicioExternoException(string mensaje, Exception? inner = null)
    : Exception(mensaje, inner);
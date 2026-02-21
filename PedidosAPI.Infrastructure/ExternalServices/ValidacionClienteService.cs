using Microsoft.Extensions.Logging;
using Polly.CircuitBreaker;
using PedidosAPI.Application.Exceptions;
using PedidosAPI.Application.Interfaces;

namespace PedidosAPI.Infrastructure.ExternalServices;

/// <summary>
/// Valida la existencia de un cliente usando la API pública de JSONPlaceholder.
/// Las políticas de resiliencia (Circuit Breaker + Retry + Timeout) están
/// configuradas en InfrastructureServiceExtensions via Polly.
///
/// Estados del Circuit Breaker:
///   CLOSED    → funcionamiento normal, las llamadas pasan.
///   OPEN      → circuito abierto, las llamadas fallan de inmediato
///               con BrokenCircuitException sin contactar el servicio.
///   HALF-OPEN → se permite una llamada de prueba para verificar recuperación.
/// </summary>
public sealed class ValidacionClienteService(
    HttpClient httpClient,
    ILogger<ValidacionClienteService> logger) : IValidacionClienteService
{
    public async Task<bool> ValidarClienteAsync(int clienteId, CancellationToken ct = default)
    {
        logger.LogInformation(
            "Validando ClienteId={ClienteId} con servicio externo.", clienteId);

        HttpResponseMessage response;

        try
        {
            // Polly intercepta esta llamada y aplica el pipeline:
            // Timeout total → Circuit Breaker → Retry → Timeout por intento
            response = await httpClient.GetAsync($"users/{clienteId}", ct);
        }
        catch (BrokenCircuitException ex)
        {
            // El circuito está ABIERTO: Polly rechaza la llamada sin ir a la red
            logger.LogError(
                "[CIRCUIT BREAKER ABIERTO] Servicio no disponible. " +
                "ClienteId={ClienteId}. Error: {Mensaje}",
                clienteId, ex.Message);

            throw new ServicioExternoException(
                "El servicio de validación no está disponible (Circuit Breaker abierto). " +
                "Intente nuevamente en unos segundos.", ex);
        }
        catch (HttpRequestException ex)
        {
            logger.LogError(ex,
                "Error de red al contactar el servicio de validación. ClienteId={ClienteId}",
                clienteId);

            throw new ServicioExternoException(
                "Error de red al contactar el servicio de validación de cliente.", ex);
        }
        catch (TaskCanceledException ex) when (!ct.IsCancellationRequested)
        {
            logger.LogError(ex,
                "Timeout al contactar el servicio de validación. ClienteId={ClienteId}",
                clienteId);

            throw new ServicioExternoException(
                "Tiempo de espera agotado al contactar el servicio de validación.", ex);
        }

        if (response.IsSuccessStatusCode)
        {
            logger.LogInformation(
                "ClienteId={ClienteId} validado correctamente. Status={Status}",
                clienteId, (int)response.StatusCode);
            return true;
        }

        if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
        {
            logger.LogWarning(
                "ClienteId={ClienteId} no encontrado en el servicio externo.", clienteId);
            return false;
        }

        logger.LogError(
            "El servicio externo retornó un error inesperado. " +
            "ClienteId={ClienteId} Status={Status}",
            clienteId, (int)response.StatusCode);

        throw new ServicioExternoException(
            $"El servicio externo retornó un error inesperado: {(int)response.StatusCode} {response.StatusCode}.");
    }
}
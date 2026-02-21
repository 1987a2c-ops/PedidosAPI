using Microsoft.Extensions.Logging;
using PedidosAPI.Application.Exceptions;
using PedidosAPI.Application.Interfaces;

namespace PedidosAPI.Infrastructure.ExternalServices;

public sealed class ValidacionClienteService(
    HttpClient httpClient,
    ILogger<ValidacionClienteService> logger) : IValidacionClienteService
{
    public async Task<bool> ValidarClienteAsync(int clienteId, CancellationToken ct = default)
    {
        logger.LogInformation("Validando ClienteId={ClienteId} con servicio externo.", clienteId);

        HttpResponseMessage response;
        try
        {
            // La URL base está configurada en DI: https://jsonplaceholder.typicode.com/
            response = await httpClient.GetAsync($"users/{clienteId}", ct);
        }
        catch (HttpRequestException ex)
        {
            throw new ServicioExternoException("Error de red al contactar el servicio de validación.", ex);
        }
        catch (TaskCanceledException ex) when (!ct.IsCancellationRequested)
        {
            throw new ServicioExternoException("Timeout al contactar el servicio de validación.", ex);
        }

        if (response.IsSuccessStatusCode)
        {
            logger.LogInformation("ClienteId={ClienteId} validado correctamente.", clienteId);
            return true;
        }

        if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
        {
            logger.LogWarning("ClienteId={ClienteId} no encontrado en servicio externo.", clienteId);
            return false;
        }

        throw new ServicioExternoException(
            $"El servicio externo retornó un error inesperado: {(int)response.StatusCode} {response.StatusCode}.");
    }
}
using System.Net;
using System.Text.Json;
using PedidosAPI.Application.Exceptions;

namespace PedidosAPI.API.Middleware;

public sealed class ExceptionMiddleware(RequestDelegate next, ILogger<ExceptionMiddleware> logger)
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await next(context);
        }
        catch (Exception ex)
        {
            await HandleAsync(context, ex);
        }
    }

    private async Task HandleAsync(HttpContext context, Exception exception)
    {
        logger.LogError(exception, "Excepción no controlada: {Mensaje}", exception.Message);

        var (status, error) = exception switch
        {
            PedidoInvalidoException e => (HttpStatusCode.BadRequest, e.Message),
            ClienteNoValidoException e => (HttpStatusCode.UnprocessableEntity, e.Message),
            ServicioExternoException e => (HttpStatusCode.ServiceUnavailable, e.Message),
            _ => (HttpStatusCode.InternalServerError, "Ocurrió un error interno inesperado.")
        };

        context.Response.ContentType = "application/json";
        context.Response.StatusCode = (int)status;

        var body = JsonSerializer.Serialize(new
        {
            status = (int)status,
            error = status.ToString(),
            mensaje = error,
            timestamp = DateTime.UtcNow
        }, JsonOptions);

        await context.Response.WriteAsync(body);
    }
}
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Polly;
using Polly.CircuitBreaker;
using Polly.Extensions.Http;
using PedidosAPI.Application.Interfaces;
using PedidosAPI.Domain.Interfaces;
using PedidosAPI.Infrastructure.Data;
using PedidosAPI.Infrastructure.ExternalServices;
using PedidosAPI.Infrastructure.Repositories;

namespace PedidosAPI.Infrastructure.DependencyInjection;

public static class InfrastructureServiceExtensions
{
    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services, IConfiguration configuration)
    {
        //  Entity Framework Core
        services.AddDbContext<AppDbContext>(options =>
            options.UseSqlServer(
                configuration.GetConnectionString("DefaultConnection")));

        //  Repositories 
        services.AddScoped<IPedidoRepository, PedidoRepository>();
        services.AddScoped<IAuditoriaRepository, AuditoriaRepository>();

        //  Unit of Work 
        services.AddScoped<IUnitOfWork, UnitOfWork>();

        //  HttpClient + Polly: Circuit Breaker + Retry + Timeout 
        
        services.AddHttpClient<IValidacionClienteService, ValidacionClienteService>(client =>
        {
            client.BaseAddress = new Uri("https://jsonplaceholder.typicode.com/");
            client.Timeout = TimeSpan.FromSeconds(30);
        })
        .AddPolicyHandler((serviceProvider, _) => GetTimeoutPolicy(serviceProvider))
        .AddPolicyHandler((serviceProvider, _) => GetCircuitBreakerPolicy(serviceProvider))
        .AddPolicyHandler((serviceProvider, _) => GetRetryPolicy(serviceProvider))
        .AddPolicyHandler((serviceProvider, _) => GetAttemptTimeoutPolicy(serviceProvider));

        return services;
    }

    //  1. Timeout total
    private static IAsyncPolicy<HttpResponseMessage> GetTimeoutPolicy(IServiceProvider sp)
    {
        var logger = sp.GetRequiredService<ILogger<ValidacionClienteService>>();

        return Policy.TimeoutAsync<HttpResponseMessage>(
            seconds: 10,
            onTimeoutAsync: (_, timespan, _) =>
            {
                logger.LogError(
                    "[TIMEOUT TOTAL] La operación completa superó {Segundos}s.", timespan.TotalSeconds);
                return Task.CompletedTask;
            });
    }

    //  2. Circuit Breaker
    //
    //  CLOSED   → funcionamiento normal.
    //  OPEN     → tras 3 fallos consecutivos se abre 15 seg; las llamadas fallan
    //             de inmediato con BrokenCircuitException sin tocar la red.
    //  HALF-OPEN→ después de 15 seg permite una llamada de prueba.
    private static IAsyncPolicy<HttpResponseMessage> GetCircuitBreakerPolicy(IServiceProvider sp)
    {
        var logger = sp.GetRequiredService<ILogger<ValidacionClienteService>>();

        return HttpPolicyExtensions
            .HandleTransientHttpError()          // 5xx + HttpRequestException
            .CircuitBreakerAsync(
                handledEventsAllowedBeforeBreaking: 3,
                durationOfBreak: TimeSpan.FromSeconds(15),
                onBreak: (outcome, duration) =>
                {
                    logger.LogError(
                        "[CIRCUIT BREAKER] Estado: ABIERTO durante {Segundos}s. " +
                        "Causa: {Error}",
                        duration.TotalSeconds,
                        outcome.Exception?.Message ?? outcome.Result?.StatusCode.ToString());
                },
                onReset: () =>
                {
                    logger.LogInformation(
                        "[CIRCUIT BREAKER] Estado: CERRADO. El servicio externo se recuperó.");
                },
                onHalfOpen: () =>
                {
                    logger.LogWarning(
                        "[CIRCUIT BREAKER] Estado: SEMI-ABIERTO. Enviando petición de prueba.");
                });
    }

    //  3. Retry con backoff exponencial
    //
    //  Reintenta hasta 3 veces ante errores transitorios.
    //  Esperas: ~2s → ~4s → ~8s  (+ jitter para evitar tormentas de reintentos)
    private static IAsyncPolicy<HttpResponseMessage> GetRetryPolicy(IServiceProvider sp)
    {
        var logger = sp.GetRequiredService<ILogger<ValidacionClienteService>>();

        return HttpPolicyExtensions
            .HandleTransientHttpError()
            .WaitAndRetryAsync(
                retryCount: 3,
                sleepDurationProvider: attempt =>
                {
                    // Backoff exponencial con jitter
                    var exponential = TimeSpan.FromSeconds(Math.Pow(2, attempt));
                    var jitter = TimeSpan.FromMilliseconds(new Random().Next(0, 500));
                    return exponential + jitter;
                },
                onRetry: (outcome, timespan, attempt, _) =>
                {
                    logger.LogWarning(
                        "[RETRY] Intento #{Intento} de 3. " +
                        "Esperando {Segundos:F1}s antes de reintentar. " +
                        "Causa: {Error}",
                        attempt,
                        timespan.TotalSeconds,
                        outcome.Exception?.Message ?? outcome.Result?.StatusCode.ToString());
                });
    }

    //  4. Timeout por intento individual 
    //  Cada intento (incluyendo reintentos) tiene 5 seg como límite máximo.
    private static IAsyncPolicy<HttpResponseMessage> GetAttemptTimeoutPolicy(IServiceProvider sp)
    {
        var logger = sp.GetRequiredService<ILogger<ValidacionClienteService>>();

        return Policy.TimeoutAsync<HttpResponseMessage>(
            seconds: 5,
            onTimeoutAsync: (_, timespan, _) =>
            {
                logger.LogWarning(
                    "[TIMEOUT INTENTO] Un intento individual superó {Segundos}s.", timespan.TotalSeconds);
                return Task.CompletedTask;
            });
    }
}
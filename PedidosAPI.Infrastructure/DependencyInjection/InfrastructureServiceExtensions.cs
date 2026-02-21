using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
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
        // Entity Framework Core + SQL Server
        services.AddDbContext<AppDbContext>(options =>
            options.UseSqlServer(
                configuration.GetConnectionString("DefaultConnection"),
                sql => sql.EnableRetryOnFailure(maxRetryCount: 3,
                    maxRetryDelay: TimeSpan.FromSeconds(5), errorNumbersToAdd: null)));

        // Repositories
        services.AddScoped<IPedidoRepository, PedidoRepository>();
        services.AddScoped<IAuditoriaRepository, AuditoriaRepository>();

        // Unit of Work
        services.AddScoped<IUnitOfWork, UnitOfWork>();

        // External HTTP service
        services.AddHttpClient<IValidacionClienteService, ValidacionClienteService>(client =>
        {
            client.BaseAddress = new Uri("https://jsonplaceholder.typicode.com/");
            client.Timeout = TimeSpan.FromSeconds(10);
        });

        return services;
    }
}
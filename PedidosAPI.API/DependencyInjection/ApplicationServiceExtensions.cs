using FluentValidation;
using PedidosAPI.Application.DTOs;
using PedidosAPI.Application.Interfaces;
using PedidosAPI.Application.UseCases;
using PedidosAPI.Application.Validators;

namespace PedidosAPI.API.DependencyInjection;

public static class ApplicationServiceExtensions
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.AddScoped<IPedidoService, RegistrarPedidoUseCase>();
        services.AddScoped<IValidator<CrearPedidoRequest>, CrearPedidoRequestValidator>();
        return services;
    }
}
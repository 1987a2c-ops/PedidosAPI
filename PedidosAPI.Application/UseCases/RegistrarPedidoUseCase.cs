using FluentValidation;
using Microsoft.Extensions.Logging;
using PedidosAPI.Application.DTOs;
using PedidosAPI.Application.Exceptions;
using PedidosAPI.Application.Interfaces;
using PedidosAPI.Domain.Entities;
using PedidosAPI.Domain.Interfaces;

namespace PedidosAPI.Application.UseCases;

public sealed class RegistrarPedidoUseCase(
    IPedidoRepository pedidoRepository,
    IAuditoriaRepository auditoria,
    IValidacionClienteService validacionCliente,
    IUnitOfWork unitOfWork,
    IValidator<CrearPedidoRequest> validator,
    ILogger<RegistrarPedidoUseCase> logger) : IPedidoService
{
    public async Task<CrearPedidoResponse> RegistrarPedidoAsync(
        CrearPedidoRequest request, CancellationToken ct = default)
    {
        //  1. Validar request 
        var validacion = await validator.ValidateAsync(request, ct);
        if (!validacion.IsValid)
        {
            var errores = string.Join("; ", validacion.Errors.Select(e => e.ErrorMessage));
            throw new PedidoInvalidoException(errores);
        }

        logger.LogInformation("Iniciando registro de pedido. ClienteId={ClienteId} Usuario={Usuario}",
            request.ClienteId, request.Usuario);

        //  2. Abrir transacción 
        await unitOfWork.BeginTransactionAsync(ct);

        try
        {
            //  3. Auditoría: inicio 
            await auditoria.RegistrarAsync(
                evento: "PEDIDO_INICIO",
                descripcion: $"Inicio de registro para ClienteId={request.ClienteId}",
                usuario: request.Usuario, ct: ct);

            //  4. Validar cliente con servicio externo ─
            bool clienteValido;
            try
            {
                clienteValido = await validacionCliente.ValidarClienteAsync(request.ClienteId, ct);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error al contactar servicio externo para ClienteId={ClienteId}", request.ClienteId);

                await auditoria.RegistrarAsync(
                    evento: "VALIDACION_ERROR",
                    descripcion: $"Error en servicio externo: {ex.Message}",
                    usuario: request.Usuario, nivel: "ERROR", ct: ct);

                throw new ServicioExternoException("No se pudo contactar el servicio de validación de cliente.", ex);
            }

            if (!clienteValido)
            {
                await auditoria.RegistrarAsync(
                    evento: "CLIENTE_INVALIDO",
                    descripcion: $"ClienteId={request.ClienteId} no superó la validación externa.",
                    usuario: request.Usuario, nivel: "WARNING", ct: ct);

                throw new ClienteNoValidoException(request.ClienteId);
            }

            //  5. Construir entidad 
            var detalles = request.Items.Select(i => new PedidoDetalle
            {
                ProductoId = i.ProductoId,
                Cantidad = i.Cantidad,
                Precio = i.Precio
            }).ToList();

            var pedido = new PedidoCabecera
            {
                ClienteId = request.ClienteId,
                Usuario = request.Usuario,
                Fecha = DateTime.UtcNow,
                Total = detalles.Sum(d => d.Subtotal),
                Detalles = detalles
            };

            //  6. Persistir 
            var creado = await pedidoRepository.CrearAsync(pedido, ct);

            //  7. Auditoría: éxito 
            await auditoria.RegistrarAsync(
                evento: "PEDIDO_CONFIRMADO",
                descripcion: $"Pedido #{creado.Id} registrado. Total={creado.Total:C}",
                usuario: request.Usuario, ct: ct);

            //  8. Confirmar transacción 
            await unitOfWork.CommitAsync(ct);

            logger.LogInformation("Pedido #{PedidoId} confirmado. Total={Total}", creado.Id, creado.Total);

            //  9. Mapear y retornar 
            return new CrearPedidoResponse(
                PedidoId: creado.Id,
                ClienteId: creado.ClienteId,
                Usuario: creado.Usuario,
                Fecha: creado.Fecha,
                Total: creado.Total,
                Items: creado.Detalles.Select(d =>
                    new ItemPedidoResponseDto(d.ProductoId, d.Cantidad, d.Precio, d.Subtotal))
            );
        }
        catch
        {
            logger.LogWarning("Realizando rollback de la transacción.");
            await unitOfWork.RollbackAsync(ct);
            throw;
        }
    }
}
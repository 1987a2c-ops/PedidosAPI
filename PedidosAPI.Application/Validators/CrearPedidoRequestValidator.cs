using FluentValidation;
using PedidosAPI.Application.DTOs;

namespace PedidosAPI.Application.Validators;

public class CrearPedidoRequestValidator : AbstractValidator<CrearPedidoRequest>
{
    public CrearPedidoRequestValidator()
    {
        RuleFor(x => x.ClienteId)
            .GreaterThan(0).WithMessage("ClienteId debe ser mayor a 0.");

        RuleFor(x => x.Usuario)
            .NotEmpty().WithMessage("El usuario es obligatorio.")
            .MaximumLength(100).WithMessage("El usuario no puede superar 100 caracteres.");

        RuleFor(x => x.Items)
            .NotEmpty().WithMessage("El pedido debe contener al menos un ítem.");

        RuleForEach(x => x.Items).ChildRules(item =>
        {
            item.RuleFor(i => i.ProductoId)
                .GreaterThan(0).WithMessage("ProductoId debe ser mayor a 0.");
            item.RuleFor(i => i.Cantidad)
                .GreaterThan(0).WithMessage("La cantidad debe ser mayor a 0.");
            item.RuleFor(i => i.Precio)
                .GreaterThan(0).WithMessage("El precio debe ser mayor a 0.");
        });
    }
}
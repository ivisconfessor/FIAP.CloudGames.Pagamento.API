using FIAP.CloudGames.Pagamento.API.Application.DTOs;
using FluentValidation;

namespace FIAP.CloudGames.Pagamento.API.Application.Validators;

public class CreatePaymentDtoValidator : AbstractValidator<CreatePaymentDto>
{
    public CreatePaymentDtoValidator()
    {
        RuleFor(x => x.GameId)
            .NotEmpty().WithMessage("O ID do jogo é obrigatório.");

        RuleFor(x => x.PaymentMethod)
            .NotEmpty().WithMessage("O método de pagamento é obrigatório.")
            .Must(BeValidPaymentMethod).WithMessage("Método de pagamento inválido.");
    }

    private bool BeValidPaymentMethod(string method)
    {
        return Enum.TryParse<Domain.Entities.PaymentMethod>(method, true, out _);
    }
}

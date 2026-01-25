using FIAP.CloudGames.Pagamento.API.Domain.Events;
using MassTransit;
using Serilog;

namespace FIAP.CloudGames.Pagamento.API.Infrastructure.Consumers;

public class PaymentFailedEventConsumer : IConsumer<PaymentFailedEvent>
{
    private readonly ILogger<PaymentFailedEventConsumer> _logger;

    public PaymentFailedEventConsumer(ILogger<PaymentFailedEventConsumer> logger)
    {
        _logger = logger;
    }

    public async Task Consume(ConsumeContext<PaymentFailedEvent> context)
    {
        var @event = context.Message;
        
        _logger.LogWarning(
            "Evento PaymentFailed recebido. PaymentId: {PaymentId}, UserId: {UserId}, GameId: {GameId}, ErrorMessage: {ErrorMessage}",
            @event.PaymentId,
            @event.UserId,
            @event.GameId,
            @event.ErrorMessage
        );

        try
        {
            await Task.Delay(100); // Simular processamento
            
            _logger.LogInformation(
                "Falha de pagamento {PaymentId} processada. Usuário será notificado.",
                @event.PaymentId
            );
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao processar PaymentFailedEvent para pagamento {PaymentId}", @event.PaymentId);
            throw;
        }
    }
}

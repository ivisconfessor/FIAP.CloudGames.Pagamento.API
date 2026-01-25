using FIAP.CloudGames.Pagamento.API.Domain.Events;
using MassTransit;
using Serilog;

namespace FIAP.CloudGames.Pagamento.API.Infrastructure.Consumers;

public class PaymentCompletedEventConsumer : IConsumer<PaymentCompletedEvent>
{
    private readonly ILogger<PaymentCompletedEventConsumer> _logger;

    public PaymentCompletedEventConsumer(ILogger<PaymentCompletedEventConsumer> logger)
    {
        _logger = logger;
    }

    public async Task Consume(ConsumeContext<PaymentCompletedEvent> context)
    {
        var @event = context.Message;
        
        _logger.LogInformation(
            "Evento PaymentCompleted recebido. PaymentId: {PaymentId}, UserId: {UserId}, GameId: {GameId}, TransactionId: {TransactionId}",
            @event.PaymentId,
            @event.UserId,
            @event.GameId,
            @event.TransactionId
        );

        try
        {
            await Task.Delay(100); // Simular processamento
            
            _logger.LogInformation(
                "Pagamento {PaymentId} completado e processado com sucesso",
                @event.PaymentId
            );
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao processar PaymentCompletedEvent para pagamento {PaymentId}", @event.PaymentId);
            throw;
        }
    }
}

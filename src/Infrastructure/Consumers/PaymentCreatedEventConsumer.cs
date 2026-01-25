using FIAP.CloudGames.Pagamento.API.Domain.Events;
using MassTransit;
using Serilog;

namespace FIAP.CloudGames.Pagamento.API.Infrastructure.Consumers;

public class PaymentCreatedEventConsumer : IConsumer<PaymentCreatedEvent>
{
    private readonly ILogger<PaymentCreatedEventConsumer> _logger;

    public PaymentCreatedEventConsumer(ILogger<PaymentCreatedEventConsumer> logger)
    {
        _logger = logger;
    }

    public async Task Consume(ConsumeContext<PaymentCreatedEvent> context)
    {
        var @event = context.Message;
        
        _logger.LogInformation(
            "Evento PaymentCreated recebido. PaymentId: {PaymentId}, UserId: {UserId}, GameId: {GameId}, Amount: {Amount}",
            @event.PaymentId,
            @event.UserId,
            @event.GameId,
            @event.Amount
        );

        try
        {
            await Task.Delay(100); // Simular processamento
            
            _logger.LogInformation(
                "Pagamento {PaymentId} processado com sucesso no consumer",
                @event.PaymentId
            );
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao processar PaymentCreatedEvent para pagamento {PaymentId}", @event.PaymentId);
            throw; // Reenviar a mensagem para Dead Letter Queue
        }
    }
}

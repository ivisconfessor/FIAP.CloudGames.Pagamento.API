using FIAP.CloudGames.Pagamento.API.Domain.Events;
using MassTransit;
using Serilog;

namespace FIAP.CloudGames.Pagamento.API.Infrastructure.Consumers;

public class GamePurchasedEventConsumer : IConsumer<GamePurchasedEvent>
{
    private readonly ILogger<GamePurchasedEventConsumer> _logger;

    public GamePurchasedEventConsumer(ILogger<GamePurchasedEventConsumer> logger)
    {
        _logger = logger;
    }

    public async Task Consume(ConsumeContext<GamePurchasedEvent> context)
    {
        var @event = context.Message;
        
        _logger.LogInformation(
            "Evento GamePurchased recebido. UserId: {UserId}, GameId: {GameId}, Price: {Price}",
            @event.UserId,
            @event.GameId,
            @event.Price
        );

        try
        {
            await Task.Delay(100); // Simular processamento
            
            _logger.LogInformation(
                "Jogo {GameId} foi adquirido com sucesso por usuário {UserId}",
                @event.GameId,
                @event.UserId
            );
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao processar GamePurchasedEvent para game {GameId}", @event.GameId);
            throw;
        }
    }
}

using FIAP.CloudGames.Pagamento.API.Domain.Entities;

namespace FIAP.CloudGames.Pagamento.API.Infrastructure.Services;

public interface IPaymentProcessingService
{
    Task<(bool Success, string? TransactionId, string? ErrorMessage)> ProcessPaymentAsync(Payment payment);
}

public class PaymentProcessingService : IPaymentProcessingService
{
    private readonly ILogger<PaymentProcessingService> _logger;

    public PaymentProcessingService(ILogger<PaymentProcessingService> logger)
    {
        _logger = logger;
    }

    public async Task<(bool Success, string? TransactionId, string? ErrorMessage)> ProcessPaymentAsync(Payment payment)
    {
        _logger.LogInformation("Processando pagamento {PaymentId} com método {Method}", payment.Id, payment.Method);

        // Simulação de processamento assíncrono
        await Task.Delay(2000); // Simula tempo de processamento

        // Simulação de sucesso/falha (90% de sucesso)
        var random = new Random();
        var success = random.Next(100) < 90;

        if (success)
        {
            var transactionId = $"TXN-{Guid.NewGuid().ToString()[..8].ToUpper()}";
            _logger.LogInformation("Pagamento {PaymentId} processado com sucesso. TransactionId: {TransactionId}", 
                payment.Id, transactionId);
            return (true, transactionId, null);
        }
        else
        {
            var errorMessage = "Falha ao processar pagamento. Tente novamente.";
            _logger.LogWarning("Pagamento {PaymentId} falhou: {ErrorMessage}", payment.Id, errorMessage);
            return (false, null, errorMessage);
        }
    }
}

using System.ComponentModel.DataAnnotations;

namespace FIAP.CloudGames.Pagamento.API.Domain.Entities;

public class Payment
{
    public Guid Id { get; private set; }
    
    [Required]
    public Guid UserId { get; private set; }
    
    [Required]
    public Guid GameId { get; private set; }
    
    [Required]
    public decimal Amount { get; private set; }
    
    public PaymentStatus Status { get; private set; }
    
    public PaymentMethod Method { get; private set; }
    
    public string? TransactionId { get; private set; }
    
    public string? ErrorMessage { get; private set; }
    
    public DateTime CreatedAt { get; private set; }
    public DateTime? ProcessedAt { get; private set; }

    private Payment() { } // Para o EF Core

    public Payment(Guid userId, Guid gameId, decimal amount, PaymentMethod method)
    {
        Id = Guid.NewGuid();
        UserId = userId;
        GameId = gameId;
        Amount = amount;
        Method = method;
        Status = PaymentStatus.Pending;
        CreatedAt = DateTime.UtcNow;
    }

    public void MarkAsProcessing()
    {
        Status = PaymentStatus.Processing;
    }

    public void MarkAsCompleted(string transactionId)
    {
        Status = PaymentStatus.Completed;
        TransactionId = transactionId;
        ProcessedAt = DateTime.UtcNow;
    }

    public void MarkAsFailed(string errorMessage)
    {
        Status = PaymentStatus.Failed;
        ErrorMessage = errorMessage;
        ProcessedAt = DateTime.UtcNow;
    }

    public void MarkAsCancelled()
    {
        Status = PaymentStatus.Cancelled;
        ProcessedAt = DateTime.UtcNow;
    }
}

public enum PaymentStatus
{
    Pending,
    Processing,
    Completed,
    Failed,
    Cancelled
}

public enum PaymentMethod
{
    CreditCard,
    DebitCard,
    Pix,
    Boleto,
    PayPal
}

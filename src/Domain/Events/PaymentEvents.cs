namespace FIAP.CloudGames.Pagamento.API.Domain.Events;

public record PaymentCreatedEvent(
    Guid PaymentId,
    Guid UserId,
    Guid GameId,
    decimal Amount,
    string Method,
    DateTime CreatedAt
);

public record PaymentProcessingEvent(
    Guid PaymentId,
    Guid UserId,
    Guid GameId,
    DateTime ProcessingAt
);

public record PaymentCompletedEvent(
    Guid PaymentId,
    Guid UserId,
    Guid GameId,
    decimal Amount,
    string TransactionId,
    DateTime CompletedAt
);

public record PaymentFailedEvent(
    Guid PaymentId,
    Guid UserId,
    Guid GameId,
    string ErrorMessage,
    DateTime FailedAt
);

public record GamePurchasedEvent(
    Guid UserId,
    Guid GameId,
    decimal Price,
    DateTime PurchasedAt
);

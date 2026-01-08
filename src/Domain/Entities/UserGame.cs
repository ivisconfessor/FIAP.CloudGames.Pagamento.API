namespace FIAP.CloudGames.Pagamento.API.Domain.Entities;

public class UserGame
{
    public Guid Id { get; private set; }
    public Guid UserId { get; private set; }
    public Guid GameId { get; private set; }
    public Guid PaymentId { get; private set; }
    public DateTime PurchaseDate { get; private set; }
    public decimal PurchasePrice { get; private set; }

    public Payment Payment { get; private set; } = null!;

    private UserGame() { } // Para o EF Core

    public UserGame(Guid userId, Guid gameId, Guid paymentId, decimal purchasePrice)
    {
        Id = Guid.NewGuid();
        UserId = userId;
        GameId = gameId;
        PaymentId = paymentId;
        PurchasePrice = purchasePrice;
        PurchaseDate = DateTime.UtcNow;
    }
}

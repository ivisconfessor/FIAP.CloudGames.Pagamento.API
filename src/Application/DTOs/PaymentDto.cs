using System.ComponentModel.DataAnnotations;

namespace FIAP.CloudGames.Pagamento.API.Application.DTOs;

public record CreatePaymentDto(
    [Required] Guid GameId,
    [Required] string PaymentMethod);

public record PaymentResponseDto(
    Guid Id,
    Guid UserId,
    Guid GameId,
    decimal Amount,
    string Status,
    string Method,
    string? TransactionId,
    string? ErrorMessage,
    DateTime CreatedAt,
    DateTime? ProcessedAt);

public record ProcessPaymentDto(
    [Required] Guid PaymentId);

public record GameDto(
    Guid Id,
    string Title,
    string Description,
    decimal Price);

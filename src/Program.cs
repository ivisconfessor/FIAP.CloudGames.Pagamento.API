using System.Security.Claims;
using FIAP.CloudGames.Pagamento.API.Domain.Entities;
using FIAP.CloudGames.Pagamento.API.Domain.Events;
using FIAP.CloudGames.Pagamento.API.Application.DTOs;
using FIAP.CloudGames.Pagamento.API.Infrastructure.Configurations;
using FIAP.CloudGames.Pagamento.API.Infrastructure.Data;
using FIAP.CloudGames.Pagamento.API.Infrastructure.EventSourcing;
using FIAP.CloudGames.Pagamento.API.Infrastructure.Services;
using FIAP.CloudGames.Pagamento.API.Application.Middleware;
using Microsoft.EntityFrameworkCore;
using Microsoft.OpenApi.Models;
using FluentValidation.AspNetCore;
using FluentValidation;
using FIAP.CloudGames.Pagamento.API.Application.Validators;
using Serilog;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;

// Configurar Serilog
Log.Logger = new LoggerConfiguration()
    .WriteTo.Console()
    .Enrich.FromLogContext()
    .Enrich.WithMachineName()
    .Enrich.WithThreadId()
    .CreateLogger();

var builder = WebApplication.CreateBuilder(args);

// Usar Serilog
builder.Host.UseSerilog();

// Add services to the container.
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo 
    { 
        Title = "FIAP Cloud Games - Pagamentos API", 
        Version = "v1",
        Description = "Microsserviço de processamento de pagamentos e compras de jogos"
    });
    c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Description = "JWT Authorization header using the Bearer scheme",
        Name = "Authorization",
        In = ParameterLocation.Header,
        Type = SecuritySchemeType.ApiKey,
        Scheme = "Bearer"
    });
    c.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference
                {
                    Type = ReferenceType.SecurityScheme,
                    Id = "Bearer"
                }
            },
            Array.Empty<string>()
        }
    });
});

// Registrar FluentValidation
builder.Services.AddFluentValidationAutoValidation();
builder.Services.AddValidatorsFromAssemblyContaining<CreatePaymentDtoValidator>();

// Integrar todas as dependências
builder.Services.IntegrateDependencyResolver(builder.Configuration);

// Configurar Application Insights
builder.Services.AddApplicationInsightsTelemetry();

// Configurar OpenTelemetry
builder.Services.AddOpenTelemetry()
    .WithTracing(tracerProviderBuilder =>
    {
        tracerProviderBuilder
            .SetResourceBuilder(ResourceBuilder.CreateDefault().AddService("FIAP.CloudGames.Pagamento.API"))
            .AddAspNetCoreInstrumentation()
            .AddHttpClientInstrumentation()
            .AddConsoleExporter();
    });

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment() || app.Environment.IsProduction())
{
    app.UseSwagger();
    app.UseSwaggerUI(c =>
    {
        c.SwaggerEndpoint("/swagger/v1/swagger.json", "FIAP Cloud Games - Pagamentos API v1");
        c.RoutePrefix = "swagger";
    });
}

app.UseHttpsRedirection();
app.UseCors("AllowAll");
app.UseMiddleware<ErrorHandlingMiddleware>();
app.UseAuthentication();
app.UseAuthorization();

// Payment endpoints
app.MapPost("/api/payments", async (CreatePaymentDto dto, ApplicationDbContext db, IGameApiService gameApiService, IEventStore eventStore, ClaimsPrincipal user, ILogger<Program> logger) =>
{
    var userId = Guid.Parse(user.FindFirst(ClaimTypes.NameIdentifier)?.Value);
    logger.LogInformation("Criando novo pagamento para usuário {UserId} e jogo {GameId}", userId, dto.GameId);
    
    // Verificar se o jogo existe
    var game = await gameApiService.GetGameByIdAsync(dto.GameId);
    if (game == null)
    {
        logger.LogWarning("Jogo {GameId} não encontrado", dto.GameId);
        return Results.NotFound("Jogo não encontrado");
    }

    // Verificar se o usuário já possui o jogo
    if (await db.UserGames.AnyAsync(ug => ug.UserId == userId && ug.GameId == dto.GameId))
    {
        logger.LogWarning("Usuário {UserId} já possui o jogo {GameId}", userId, dto.GameId);
        return Results.BadRequest("Você já possui este jogo");
    }

    // Criar pagamento
    if (!Enum.TryParse<PaymentMethod>(dto.PaymentMethod, true, out var paymentMethod))
    {
        return Results.BadRequest("Método de pagamento inválido");
    }

    var payment = new Payment(userId, dto.GameId, game.Price, paymentMethod);
    db.Payments.Add(payment);
    await db.SaveChangesAsync();

    // Event Sourcing
    var paymentCreatedEvent = new PaymentCreatedEvent(
        payment.Id,
        payment.UserId,
        payment.GameId,
        payment.Amount,
        payment.Method.ToString(),
        payment.CreatedAt
    );
    await eventStore.SaveEventAsync(paymentCreatedEvent);

    logger.LogInformation("Pagamento criado com sucesso. ID: {PaymentId}", payment.Id);

    return Results.Created($"/api/payments/{payment.Id}", new PaymentResponseDto(
        payment.Id, payment.UserId, payment.GameId, payment.Amount,
        payment.Status.ToString(), payment.Method.ToString(),
        payment.TransactionId, payment.ErrorMessage,
        payment.CreatedAt, payment.ProcessedAt));
})
.RequireAuthorization()
.WithName("CreatePayment")
.WithOpenApi();

app.MapPost("/api/payments/{id}/process", async (Guid id, ApplicationDbContext db, IPaymentProcessingService paymentProcessingService, IEventStore eventStore, ILogger<Program> logger) =>
{
    logger.LogInformation("Processando pagamento {PaymentId}", id);
    
    var payment = await db.Payments.FindAsync(id);
    if (payment == null)
    {
        logger.LogWarning("Pagamento {PaymentId} não encontrado", id);
        return Results.NotFound("Pagamento não encontrado");
    }

    if (payment.Status != PaymentStatus.Pending)
    {
        logger.LogWarning("Pagamento {PaymentId} já foi processado. Status: {Status}", id, payment.Status);
        return Results.BadRequest($"Pagamento já foi processado. Status atual: {payment.Status}");
    }

    // Marcar como processando
    payment.MarkAsProcessing();
    await db.SaveChangesAsync();

    // Event Sourcing
    var paymentProcessingEvent = new PaymentProcessingEvent(
        payment.Id,
        payment.UserId,
        payment.GameId,
        DateTime.UtcNow
    );
    await eventStore.SaveEventAsync(paymentProcessingEvent);

    // Processar pagamento
    var (success, transactionId, errorMessage) = await paymentProcessingService.ProcessPaymentAsync(payment);

    if (success)
    {
        payment.MarkAsCompleted(transactionId!);
        
        // Criar registro de jogo do usuário
        var userGame = new UserGame(payment.UserId, payment.GameId, payment.Id, payment.Amount);
        db.UserGames.Add(userGame);
        
        await db.SaveChangesAsync();

        // Event Sourcing
        var paymentCompletedEvent = new PaymentCompletedEvent(
            payment.Id,
            payment.UserId,
            payment.GameId,
            payment.Amount,
            transactionId!,
            DateTime.UtcNow
        );
        await eventStore.SaveEventAsync(paymentCompletedEvent);

        var gamePurchasedEvent = new GamePurchasedEvent(
            payment.UserId,
            payment.GameId,
            payment.Amount,
            DateTime.UtcNow
        );
        await eventStore.SaveEventAsync(gamePurchasedEvent);

        logger.LogInformation("Pagamento {PaymentId} processado com sucesso", id);
    }
    else
    {
        payment.MarkAsFailed(errorMessage!);
        await db.SaveChangesAsync();

        // Event Sourcing
        var paymentFailedEvent = new PaymentFailedEvent(
            payment.Id,
            payment.UserId,
            payment.GameId,
            errorMessage!,
            DateTime.UtcNow
        );
        await eventStore.SaveEventAsync(paymentFailedEvent);

        logger.LogWarning("Pagamento {PaymentId} falhou: {ErrorMessage}", id, errorMessage);
    }

    return Results.Ok(new PaymentResponseDto(
        payment.Id, payment.UserId, payment.GameId, payment.Amount,
        payment.Status.ToString(), payment.Method.ToString(),
        payment.TransactionId, payment.ErrorMessage,
        payment.CreatedAt, payment.ProcessedAt));
})
.RequireAuthorization()
.WithName("ProcessPayment")
.WithOpenApi();

app.MapGet("/api/payments/{id}", async (Guid id, ApplicationDbContext db, ClaimsPrincipal user, ILogger<Program> logger) =>
{
    logger.LogInformation("Buscando pagamento {PaymentId}", id);
    
    var userId = Guid.Parse(user.FindFirst(ClaimTypes.NameIdentifier)?.Value);
    var payment = await db.Payments.FindAsync(id);
    
    if (payment == null)
    {
        logger.LogWarning("Pagamento {PaymentId} não encontrado", id);
        return Results.NotFound("Pagamento não encontrado");
    }

    // Verificar se o usuário tem permissão para ver este pagamento
    if (payment.UserId != userId && user.FindFirst(ClaimTypes.Role)?.Value != "Admin")
    {
        logger.LogWarning("Usuário {UserId} tentou acessar pagamento {PaymentId} de outro usuário", userId, id);
        return Results.Forbid();
    }

    return Results.Ok(new PaymentResponseDto(
        payment.Id, payment.UserId, payment.GameId, payment.Amount,
        payment.Status.ToString(), payment.Method.ToString(),
        payment.TransactionId, payment.ErrorMessage,
        payment.CreatedAt, payment.ProcessedAt));
})
.RequireAuthorization()
.WithName("GetPayment")
.WithOpenApi();

app.MapGet("/api/payments", async (ApplicationDbContext db, ClaimsPrincipal user, ILogger<Program> logger) =>
{
    var userId = Guid.Parse(user.FindFirst(ClaimTypes.NameIdentifier)?.Value);
    logger.LogInformation("Listando pagamentos do usuário {UserId}", userId);
    
    var payments = await db.Payments
        .Where(p => p.UserId == userId)
        .Select(p => new PaymentResponseDto(
            p.Id, p.UserId, p.GameId, p.Amount,
            p.Status.ToString(), p.Method.ToString(),
            p.TransactionId, p.ErrorMessage,
            p.CreatedAt, p.ProcessedAt))
        .ToListAsync();

    return Results.Ok(payments);
})
.RequireAuthorization()
.WithName("GetUserPayments")
.WithOpenApi();

app.MapGet("/api/users/games", async (ApplicationDbContext db, ClaimsPrincipal user, ILogger<Program> logger) =>
{
    var userId = Guid.Parse(user.FindFirst(ClaimTypes.NameIdentifier)?.Value);
    logger.LogInformation("Listando jogos do usuário {UserId}", userId);
    
    var userGames = await db.UserGames
        .Where(ug => ug.UserId == userId)
        .Select(ug => new
        {
            ug.Id,
            ug.GameId,
            ug.PurchaseDate,
            ug.PurchasePrice
        })
        .ToListAsync();

    return Results.Ok(userGames);
})
.RequireAuthorization()
.WithName("GetUserGames")
.WithOpenApi();

app.MapGet("/api/payments/analytics/revenue", async (ApplicationDbContext db, ILogger<Program> logger) =>
{
    logger.LogInformation("Calculando receita total");
    
    var totalRevenue = await db.Payments
        .Where(p => p.Status == PaymentStatus.Completed)
        .SumAsync(p => p.Amount);

    var paymentsByStatus = await db.Payments
        .GroupBy(p => p.Status)
        .Select(g => new { Status = g.Key.ToString(), Count = g.Count() })
        .ToListAsync();

    var paymentsByMethod = await db.Payments
        .Where(p => p.Status == PaymentStatus.Completed)
        .GroupBy(p => p.Method)
        .Select(g => new { Method = g.Key.ToString(), Count = g.Count(), Total = g.Sum(p => p.Amount) })
        .ToListAsync();

    return Results.Ok(new
    {
        TotalRevenue = totalRevenue,
        PaymentsByStatus = paymentsByStatus,
        PaymentsByMethod = paymentsByMethod
    });
})
.RequireAuthorization()
.WithName("GetRevenueAnalytics")
.WithOpenApi();

app.MapGet("/api/health", () => Results.Ok(new { status = "healthy", service = "pagamentos", timestamp = DateTime.UtcNow }))
.AllowAnonymous()
.WithName("HealthCheck")
.WithOpenApi();

app.MapGet("/api/events/{aggregateId}", async (Guid aggregateId, IEventStore eventStore, ILogger<Program> logger) =>
{
    logger.LogInformation("Buscando eventos para aggregate ID: {AggregateId}", aggregateId);
    
    var events = await eventStore.GetEventsAsync(aggregateId);
    return Results.Ok(events);
})
.RequireAuthorization()
.WithName("GetEvents")
.WithOpenApi();

Log.Information("Iniciando FIAP.CloudGames.Pagamento.API...");

app.Run();

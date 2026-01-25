using System.Text;
using FIAP.CloudGames.Pagamento.API.Infrastructure.Data;
using FIAP.CloudGames.Pagamento.API.Infrastructure.EventSourcing;
using FIAP.CloudGames.Pagamento.API.Infrastructure.Services;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using MassTransit;

namespace FIAP.CloudGames.Pagamento.API.Infrastructure.Configurations;

public static class DependencyResolverConfigurationExtensions
{
    public static void IntegrateDependencyResolver(this IServiceCollection services, IConfiguration configuration)
    {
        // Configuração do DbContext
        services.AddDbContext<ApplicationDbContext>(options =>
            options.UseInMemoryDatabase("FIAPCloudGamesPagamentos"));

        // Configuração do JWT
        services.AddAuthentication(options =>
        {
            options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
            options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
        })
        .AddJwtBearer(options =>
        {
            var jwtKey = configuration["Jwt:Key"] ?? throw new InvalidOperationException("Jwt:Key não está configurado");
            var issuersKeys = configuration.GetSection("Jwt:IssuersKeys").GetChildren()
                .ToDictionary(x => x.Key, x => x.Value ?? "");

            options.TokenValidationParameters = new TokenValidationParameters
            {
                ValidateIssuer = true,
                ValidateAudience = true, // Valida se é para FIAP.CloudGames.Client
                ValidateLifetime = true,
                ValidateIssuerSigningKey = true,
                ValidIssuers =
                [
                    configuration["Jwt:Issuer"],
                    "FIAP.CloudGames.Usuario.API"
                ],
                ValidAudience = configuration["Jwt:Audience"], // FIAP.CloudGames.Client

                // Resolver dinâmico para buscar a chave correta por issuer
                IssuerSigningKeyResolver = (token, securityToken, kid, parameters) =>
                {
                    var issuer = securityToken?.Issuer;
                    
                    if (string.IsNullOrEmpty(issuer) || !issuersKeys.TryGetValue(issuer, out var key))
                    {
                        // Fallback para a chave padrão
                        return [new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey))];
                    }

                    return [new SymmetricSecurityKey(Encoding.UTF8.GetBytes(key))];
                }
            };
        });

        // Configuração da Autorização
        services.AddAuthorization();

        // Registro dos serviços
        services.AddScoped<IPaymentProcessingService, PaymentProcessingService>();
        
        // Permite acessar o contexto HTTP atual para encaminhar o token
        services.AddHttpContextAccessor();

        // Handler para encaminhar o header Authorization nas chamadas HTTP internas
        services.AddTransient<ForwardAuthTokenHandler>();

        services.AddHttpClient<IGameApiService, GameApiService>()
            .AddHttpMessageHandler<ForwardAuthTokenHandler>();
        services.AddSingleton<IEventStore, EventStore>();
        
        // Configuração do MassTransit com RabbitMQ
        services.AddMassTransit(x =>
        {
            // Registrar todos os consumers do assembly
            x.AddConsumers(typeof(DependencyResolverConfigurationExtensions).Assembly);
            
            x.UsingRabbitMq((context, cfg) =>
            {
                var rabbitMqHost = configuration["RabbitMQ:Host"] ?? "rabbitmq://localhost";
                var username = configuration["RabbitMQ:Username"] ?? "guest";
                var password = configuration["RabbitMQ:Password"] ?? "guest";

                cfg.Host(new Uri(rabbitMqHost), h =>
                {
                    h.Username(username);
                    h.Password(password);
                });

                // Configurar prefetch e concorrência
                cfg.PrefetchCount = int.TryParse(configuration["MassTransit:PreFetchCount"], out var prefetch) ? prefetch : 10;
                cfg.ConcurrentMessageLimit = int.TryParse(configuration["MassTransit:ConcurrentMessageLimit"], out var limit) ? limit : 5;

                // Configurar endpoints dos consumers
                cfg.ConfigureEndpoints(context);

                // Adicionar configuração de retry
                cfg.UseMessageRetry(r =>
                {
                    r.Interval(3, TimeSpan.FromSeconds(5)); // 3 tentativas com 5 segundos entre elas
                });
            });
        });
        
        // CORS
        services.AddCors(options =>
        {
            options.AddPolicy("AllowAll", builder =>
            {
                builder.AllowAnyOrigin()
                       .AllowAnyMethod()
                       .AllowAnyHeader();
            });
        });
    }
}

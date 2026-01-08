using System.Text.Json;
using FIAP.CloudGames.Pagamento.API.Application.DTOs;

namespace FIAP.CloudGames.Pagamento.API.Infrastructure.Services;

public interface IGameApiService
{
    Task<GameDto?> GetGameByIdAsync(Guid gameId);
}

public class GameApiService : IGameApiService
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<GameApiService> _logger;
    private readonly IConfiguration _configuration;

    public GameApiService(HttpClient httpClient, ILogger<GameApiService> logger, IConfiguration configuration)
    {
        _httpClient = httpClient;
        _logger = logger;
        _configuration = configuration;
    }

    public async Task<GameDto?> GetGameByIdAsync(Guid gameId)
    {
        try
        {
            var gameApiUrl = _configuration["ServiceUrls:JogoAPI"];
            _logger.LogInformation("Buscando jogo {GameId} na API de Jogos: {Url}", gameId, gameApiUrl);

            var response = await _httpClient.GetAsync($"{gameApiUrl}/api/games/{gameId}");
            
            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning("Jogo {GameId} não encontrado na API de Jogos", gameId);
                return null;
            }

            var content = await response.Content.ReadAsStringAsync();
            var game = JsonSerializer.Deserialize<GameDto>(content, new JsonSerializerOptions 
            { 
                PropertyNameCaseInsensitive = true 
            });

            return game;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao buscar jogo {GameId} na API de Jogos", gameId);
            return null;
        }
    }
}

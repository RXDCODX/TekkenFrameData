using Grpc.Net.Client;
using TekkenFrameData.Core.Protos;

namespace TekkenFrameData.TwitchService.Services;

public class FrameDataClient
{
    private readonly FrameDataService.FrameDataServiceClient _client;
    private readonly ILogger<FrameDataClient> _logger;

    public FrameDataClient(ILogger<FrameDataClient> logger, IConfiguration configuration)
    {
        _logger = logger;

        var coreServiceUrl = configuration["CoreService:Url"] ?? "http://localhost:5000";
        var channel = GrpcChannel.ForAddress(coreServiceUrl);
        _client = new FrameDataService.FrameDataServiceClient(channel);
    }

        public async Task<IEnumerable<TekkenCharacter>> GetCharactersAsync()
    {
        try
        {
            var response = await _client.GetCharactersAsync(new GetCharactersRequest());
            return response.Characters;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting characters from Core service");
            return Enumerable.Empty<TekkenCharacter>();
        }
    }

    public async Task<TekkenCharacter?> GetCharacterAsync(int characterId)
    {
        try
        {
            var response = await _client.GetCharacterAsync(new GetCharacterRequest 
            { 
                CharacterId = characterId 
            });
            return response.Character;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting character {CharacterId} from Core service", characterId);
            return null;
        }
    }

    public async Task<TekkenCharacter?> GetCharacterAsync(string characterName)
    {
        try
        {
            var response = await _client.GetCharacterAsync(new GetCharacterRequest 
            { 
                CharacterName = characterName 
            });
            return response.Character;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting character {CharacterName} from Core service", characterName);
            return null;
        }
    }

        public async Task<IEnumerable<TekkenMove>> GetCharacterMovesAsync(int characterId)
    {
        try
        {
            var response = await _client.GetCharacterMovesAsync(new GetCharacterMovesRequest 
            { 
                CharacterId = characterId 
            });
            return response.Moves;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting moves for character {CharacterId} from Core service", characterId);
            return Enumerable.Empty<TekkenMove>();
        }
    }

    public async Task<IEnumerable<TekkenMove>> GetCharacterMovesAsync(string characterName)
    {
        try
        {
            var response = await _client.GetCharacterMovesAsync(new GetCharacterMovesRequest 
            { 
                CharacterName = characterName 
            });
            return response.Moves;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting moves for character {CharacterName} from Core service", characterName);
            return Enumerable.Empty<TekkenMove>();
        }
    }

    public async Task<TekkenMove?> GetMoveAsync(int moveId)
    {
        try
        {
            var response = await _client.GetMoveAsync(new GetMoveRequest 
            { 
                MoveId = moveId 
            });
            return response.Move;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting move {MoveId} from Core service", moveId);
            return null;
        }
    }

    public async Task<TekkenMove?> GetMoveAsync(string moveName, int characterId = 0)
    {
        try
        {
            var response = await _client.GetMoveAsync(new GetMoveRequest 
            { 
                MoveName = moveName,
                CharacterId = characterId
            });
            return response.Move;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting move {MoveName} from Core service", moveName);
            return null;
        }
    }

    public async Task<IEnumerable<TekkenMove>> SearchMovesAsync(string query, int characterId = 0, int limit = 10)
    {
        try
        {
            var response = await _client.SearchMovesAsync(new SearchMovesRequest 
            { 
                Query = query,
                CharacterId = characterId,
                Limit = limit
            });
            return response.Moves;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error searching moves with query {Query} from Core service", query);
            return Enumerable.Empty<TekkenMove>();
        }
    }
}
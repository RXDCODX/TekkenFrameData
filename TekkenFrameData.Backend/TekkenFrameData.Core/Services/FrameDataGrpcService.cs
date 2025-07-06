using Grpc.Core;
using Microsoft.EntityFrameworkCore;
using TekkenFrameData.Core.Protos;
using TekkenFrameData.Library.DB;
using TekkenFrameData.Library.Models.FrameData;

namespace TekkenFrameData.Core.Services;

public class FrameDataGrpcService : FrameDataService.FrameDataServiceBase
{
    private readonly ILogger<FrameDataGrpcService> _logger;
    private readonly IDbContextFactory<AppDbContext> _dbContextFactory;

    public FrameDataGrpcService(
        ILogger<FrameDataGrpcService> logger,
        IDbContextFactory<AppDbContext> dbContextFactory)
    {
        _logger = logger;
        _dbContextFactory = dbContextFactory;
    }

    public override async Task<GetCharactersResponse> GetCharacters(
        GetCharactersRequest request,
        ServerCallContext context)
    {
        _logger.LogInformation("Getting all characters");

        using var dbContext = await _dbContextFactory.CreateDbContextAsync();
        var characters = await dbContext.TekkenCharacters
            .Select(c => new TekkenCharacter
            {
                Id = c.Id,
                Name = c.Name,
                ImageUrl = c.ImageUrl ?? string.Empty
            })
            .ToListAsync();

        return new GetCharactersResponse
        {
            Characters = { characters }
        };
    }

    public override async Task<GetCharacterResponse> GetCharacter(
        GetCharacterRequest request,
        ServerCallContext context)
    {
        _logger.LogInformation("Getting character: {CharacterId}", request.CharacterId);

        using var dbContext = await _dbContextFactory.CreateDbContextAsync();

        TekkenCharacter? character = null;

        if (request.CharacterId > 0)
        {
            character = await dbContext.TekkenCharacters
                .FirstOrDefaultAsync(c => c.Id == request.CharacterId);
        }
        else if (!string.IsNullOrEmpty(request.CharacterName))
        {
            character = await dbContext.TekkenCharacters
                .FirstOrDefaultAsync(c => c.Name.ToLower().Contains(request.CharacterName.ToLower()));
        }

        if (character == null)
        {
            throw new RpcException(new Status(StatusCode.NotFound, "Character not found"));
        }

        return new GetCharacterResponse
        {
            Character = new TekkenCharacter
            {
                Id = character.Id,
                Name = character.Name,
                ImageUrl = character.ImageUrl ?? string.Empty
            }
        };
    }

    public override async Task<GetCharacterMovesResponse> GetCharacterMoves(
        GetCharacterMovesRequest request,
        ServerCallContext context)
    {
        _logger.LogInformation("Getting moves for character: {CharacterId}", request.CharacterId);

        using var dbContext = await _dbContextFactory.CreateDbContextAsync();

        IQueryable<TekkenMove> movesQuery = dbContext.TekkenMoves;

        if (request.CharacterId > 0)
        {
            movesQuery = movesQuery.Where(m => m.CharacterId == request.CharacterId);
        }
        else if (!string.IsNullOrEmpty(request.CharacterName))
        {
            movesQuery = movesQuery.Where(m => m.Character.Name.ToLower().Contains(request.CharacterName.ToLower()));
        }

        var moves = await movesQuery
            .Include(m => m.Character)
            .Select(m => new TekkenMove
            {
                Id = m.Id,
                Name = m.Name,
                Input = m.Input,
                Startup = m.Startup,
                Active = m.Active,
                Recovery = m.Recovery,
                OnBlock = m.OnBlock,
                OnHit = m.OnHit,
                Damage = m.Damage,
                Properties = m.Properties ?? string.Empty,
                CharacterId = m.CharacterId,
                CharacterName = m.Character.Name,
                StartupFrame = m.StartUpFrame ?? string.Empty,
                BlockFrame = m.BlockFrame ?? string.Empty,
                HitFrame = m.HitFrame ?? string.Empty,
                CounterHitFrame = m.CounterHitFrame ?? string.Empty,
                HitLevel = m.HitLevel ?? string.Empty,
                HeatEngage = m.HeatEngage,
                Tornado = m.Tornado,
                HeatSmash = m.HeatSmash,
                PowerCrush = m.PowerCrush,
                HeatBurst = m.HeatBurst,
                Homing = m.Homing,
                ThrowMove = m.Throw,
                StanceCode = m.StanceCode ?? string.Empty,
                StanceName = m.StanceName ?? string.Empty,
                IsFromStance = m.IsFromStance
            })
            .ToListAsync();

        return new GetCharacterMovesResponse
        {
            Moves = { moves }
        };
    }

    public override async Task<GetMoveResponse> GetMove(
        GetMoveRequest request,
        ServerCallContext context)
    {
        _logger.LogInformation("Getting move: {MoveId}", request.MoveId);

        using var dbContext = await _dbContextFactory.CreateDbContextAsync();

        TekkenMove? move = null;

        if (request.MoveId > 0)
        {
            move = await dbContext.TekkenMoves
                .Include(m => m.Character)
                .FirstOrDefaultAsync(m => m.Id == request.MoveId);
        }
        else if (!string.IsNullOrEmpty(request.MoveName))
        {
            move = await dbContext.TekkenMoves
                .Include(m => m.Character)
                .FirstOrDefaultAsync(m => m.Name.ToLower().Contains(request.MoveName.ToLower()) &&
                                        (request.CharacterId == 0 || m.CharacterId == request.CharacterId));
        }

        if (move == null)
        {
            throw new RpcException(new Status(StatusCode.NotFound, "Move not found"));
        }

        return new GetMoveResponse
        {
            Move = new TekkenMove
            {
                Id = move.Id,
                Name = move.Name,
                Input = move.Input,
                Startup = move.Startup,
                Active = move.Active,
                Recovery = move.Recovery,
                OnBlock = move.OnBlock,
                OnHit = move.OnHit,
                Damage = move.Damage,
                Properties = move.Properties ?? string.Empty,
                CharacterId = move.CharacterId,
                CharacterName = move.Character.Name,
                StartupFrame = move.StartUpFrame ?? string.Empty,
                BlockFrame = move.BlockFrame ?? string.Empty,
                HitFrame = move.HitFrame ?? string.Empty,
                CounterHitFrame = move.CounterHitFrame ?? string.Empty,
                HitLevel = move.HitLevel ?? string.Empty,
                HeatEngage = move.HeatEngage,
                Tornado = move.Tornado,
                HeatSmash = move.HeatSmash,
                PowerCrush = move.PowerCrush,
                HeatBurst = move.HeatBurst,
                Homing = move.Homing,
                ThrowMove = move.Throw,
                StanceCode = move.StanceCode ?? string.Empty,
                StanceName = move.StanceName ?? string.Empty,
                IsFromStance = move.IsFromStance
            }
        };
    }

    public override async Task<SearchMovesResponse> SearchMoves(
        SearchMovesRequest request,
        ServerCallContext context)
    {
        _logger.LogInformation("Searching moves with query: {Query}", request.Query);

        using var dbContext = await _dbContextFactory.CreateDbContextAsync();

        var query = dbContext.TekkenMoves
            .Include(m => m.Character)
            .AsQueryable();

        if (request.CharacterId > 0)
        {
            query = query.Where(m => m.CharacterId == request.CharacterId);
        }

        if (!string.IsNullOrEmpty(request.Query))
        {
            var searchTerm = request.Query.ToLower();
            query = query.Where(m =>
                m.Name.ToLower().Contains(searchTerm) ||
                m.Input.ToLower().Contains(searchTerm) ||
                m.Character.Name.ToLower().Contains(searchTerm));
        }

        if (request.Limit > 0)
        {
            query = query.Take(request.Limit);
        }

        var moves = await query
            .Select(m => new TekkenMove
            {
                Id = m.Id,
                Name = m.Name,
                Input = m.Input,
                Startup = m.Startup,
                Active = m.Active,
                Recovery = m.Recovery,
                OnBlock = m.OnBlock,
                OnHit = m.OnHit,
                Damage = m.Damage,
                Properties = m.Properties ?? string.Empty,
                CharacterId = m.CharacterId,
                CharacterName = m.Character.Name,
                StartupFrame = m.StartUpFrame ?? string.Empty,
                BlockFrame = m.BlockFrame ?? string.Empty,
                HitFrame = m.HitFrame ?? string.Empty,
                CounterHitFrame = m.CounterHitFrame ?? string.Empty,
                HitLevel = m.HitLevel ?? string.Empty,
                HeatEngage = m.HeatEngage,
                Tornado = m.Tornado,
                HeatSmash = m.HeatSmash,
                PowerCrush = m.PowerCrush,
                HeatBurst = m.HeatBurst,
                Homing = m.Homing,
                ThrowMove = m.Throw,
                StanceCode = m.StanceCode ?? string.Empty,
                StanceName = m.StanceName ?? string.Empty,
                IsFromStance = m.IsFromStance
            })
            .ToListAsync();

        return new SearchMovesResponse
        {
            Moves = { moves }
        };
    }
}
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using TekkenFrameData.Library.Attributes;
using TekkenFrameData.Library.DB;
using TekkenFrameData.Library.Models.FrameData;
using TekkenFrameData.Library.Models.Identity;

namespace TekkenFrameData.Service.API.v1.MoveController;

[ApiController]
[Route("api/v1/[controller]")]
public class MoveController(AppDbContext context, ILogger<MoveController> logger) : ControllerBase
{
    /// <summary>
    /// Получить все движения
    /// </summary>
    [HttpGet]
    [RequirePermission(RolePermissions.ViewMoves)]
    public async Task<ActionResult<IEnumerable<Move>>> GetMoves()
    {
        try
        {
            var moves = await context
                .TekkenMoves.Include(m => m.Character)
                .OrderBy(m => m.CharacterName)
                .ThenBy(m => m.Command)
                .ToListAsync();

            return Ok(moves);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error getting moves");
            return StatusCode(500, new { message = "Internal server error" });
        }
    }

    /// <summary>
    /// Получить движения персонажа
    /// </summary>
    [HttpGet("character/{characterName}")]
    [RequirePermission(RolePermissions.ViewMoves)]
    public async Task<ActionResult<IEnumerable<Move>>> GetMovesByCharacter(string characterName)
    {
        try
        {
            var moves = await context
                .TekkenMoves.Include(m => m.Character)
                .Where(m =>
                    m.CharacterName.Equals(characterName, StringComparison.CurrentCultureIgnoreCase)
                )
                .OrderBy(m => m.Command)
                .ToListAsync();

            return Ok(moves);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error getting moves for character {CharacterName}", characterName);
            return StatusCode(500, new { message = "Internal server error" });
        }
    }

    /// <summary>
    /// Получить движение по ID (CharacterName + Command)
    /// </summary>
    [HttpGet("{characterName}/{command}")]
    [RequirePermission(RolePermissions.ViewMoves)]
    public async Task<ActionResult<Move>> GetMove(string characterName, string command)
    {
        try
        {
            var move = await context
                .TekkenMoves.Include(m => m.Character)
                .FirstOrDefaultAsync(m =>
                    m.CharacterName.Equals(characterName, StringComparison.CurrentCultureIgnoreCase)
                    && m.Command.Equals(command, StringComparison.CurrentCultureIgnoreCase)
                );

            return move == null
                ? (ActionResult<Move>)NotFound(new { message = "Move not found" })
                : (ActionResult<Move>)Ok(move);
        }
        catch (Exception ex)
        {
            logger.LogError(
                ex,
                "Error getting move {Command} for character {CharacterName}",
                command,
                characterName
            );
            return StatusCode(500, new { message = "Internal server error" });
        }
    }

    /// <summary>
    /// Создать новое движение
    /// </summary>
    [HttpPost]
    [RequirePermission(RolePermissions.ManageMoves)]
    public async Task<ActionResult<Move>> CreateMove([FromBody] Move move)
    {
        try
        {
            // Проверяем, существует ли персонаж
            var character = await context.TekkenCharacters.FirstOrDefaultAsync(c =>
                c.Name.Equals(move.CharacterName, StringComparison.CurrentCultureIgnoreCase)
            );

            if (character == null)
            {
                return BadRequest(new { message = "Character not found" });
            }

            // Проверяем, не существует ли уже такое движение
            var existingMove = await context.TekkenMoves.FirstOrDefaultAsync(m =>
                m.CharacterName.Equals(
                    move.CharacterName,
                    StringComparison.CurrentCultureIgnoreCase
                ) && m.Command.Equals(move.Command, StringComparison.CurrentCultureIgnoreCase)
            );

            if (existingMove != null)
            {
                return BadRequest(new { message = "Move already exists for this character" });
            }

            context.TekkenMoves.Add(move);
            await context.SaveChangesAsync();

            return CreatedAtAction(
                nameof(GetMove),
                new { characterName = move.CharacterName, command = move.Command },
                move
            );
        }
        catch (Exception ex)
        {
            logger.LogError(
                ex,
                "Error creating move {Command} for character {CharacterName}",
                move.Command,
                move.CharacterName
            );
            return StatusCode(500, new { message = "Internal server error" });
        }
    }

    /// <summary>
    /// Обновить движение
    /// </summary>
    [HttpPut("{characterName}/{command}")]
    [RequirePermission(RolePermissions.ManageMoves)]
    public async Task<IActionResult> UpdateMove(
        string characterName,
        string command,
        [FromBody] Move move
    )
    {
        try
        {
            var existingMove = await context.TekkenMoves.FirstOrDefaultAsync(m =>
                m.CharacterName.Equals(characterName, StringComparison.CurrentCultureIgnoreCase)
                && m.Command.Equals(command, StringComparison.CurrentCultureIgnoreCase)
            );

            if (existingMove == null)
            {
                return NotFound(new { message = "Move not found" });
            }

            // Обновляем поля движения
            existingMove.StanceCode = move.StanceCode;
            existingMove.StanceName = move.StanceName;
            existingMove.HeatEngage = move.HeatEngage;
            existingMove.HeatSmash = move.HeatSmash;
            existingMove.PowerCrush = move.PowerCrush;
            existingMove.Throw = move.Throw;
            existingMove.Homing = move.Homing;
            existingMove.Tornado = move.Tornado;
            existingMove.HeatBurst = move.HeatBurst;
            existingMove.RequiresHeat = move.RequiresHeat;
            existingMove.HitLevel = move.HitLevel;
            existingMove.Damage = move.Damage;
            existingMove.StartUpFrame = move.StartUpFrame;
            existingMove.BlockFrame = move.BlockFrame;
            existingMove.HitFrame = move.HitFrame;
            existingMove.CounterHitFrame = move.CounterHitFrame;
            existingMove.Notes = move.Notes;
            existingMove.IsUserChanged = true;

            await context.SaveChangesAsync();

            return Ok(existingMove);
        }
        catch (Exception ex)
        {
            logger.LogError(
                ex,
                "Error updating move {Command} for character {CharacterName}",
                command,
                characterName
            );
            return StatusCode(500, new { message = "Internal server error" });
        }
    }

    /// <summary>
    /// Удалить движение
    /// </summary>
    [HttpDelete("{characterName}/{command}")]
    [RequirePermission(RolePermissions.ManageMoves)]
    public async Task<IActionResult> DeleteMove(string characterName, string command)
    {
        try
        {
            var move = await context.TekkenMoves.FirstOrDefaultAsync(m =>
                m.CharacterName.Equals(characterName, StringComparison.CurrentCultureIgnoreCase)
                && m.Command.Equals(command, StringComparison.CurrentCultureIgnoreCase)
            );

            if (move == null)
            {
                return NotFound(new { message = "Move not found" });
            }

            context.TekkenMoves.Remove(move);
            await context.SaveChangesAsync();

            return Ok(new { message = "Move deleted successfully" });
        }
        catch (Exception ex)
        {
            logger.LogError(
                ex,
                "Error deleting move {Command} for character {CharacterName}",
                command,
                characterName
            );
            return StatusCode(500, new { message = "Internal server error" });
        }
    }

    /// <summary>
    /// Поиск движений по фильтрам
    /// </summary>
    [HttpGet("search")]
    [RequirePermission(RolePermissions.ViewMoves)]
    public async Task<ActionResult<IEnumerable<Move>>> SearchMoves(
        [FromQuery] string? characterName = null,
        [FromQuery] string? command = null,
        [FromQuery] string? hitLevel = null,
        [FromQuery] bool? heatEngage = null,
        [FromQuery] bool? powerCrush = null,
        [FromQuery] bool? throw_ = null,
        [FromQuery] bool? homing = null
    )
    {
        try
        {
            var query = context.TekkenMoves.Include(m => m.Character).AsQueryable();

            if (!string.IsNullOrEmpty(characterName))
            {
                query = query.Where(m =>
                    m.CharacterName.ToLower().Contains(characterName.ToLower())
                );
            }

            if (!string.IsNullOrEmpty(command))
            {
                query = query.Where(m => m.Command.ToLower().Contains(command.ToLower()));
            }

            if (!string.IsNullOrEmpty(hitLevel))
            {
                query = query.Where(m =>
                    m.HitLevel != null && m.HitLevel.ToLower().Contains(hitLevel.ToLower())
                );
            }

            if (heatEngage.HasValue)
            {
                query = query.Where(m => m.HeatEngage == heatEngage.Value);
            }

            if (powerCrush.HasValue)
            {
                query = query.Where(m => m.PowerCrush == powerCrush.Value);
            }

            if (throw_.HasValue)
            {
                query = query.Where(m => m.Throw == throw_.Value);
            }

            if (homing.HasValue)
            {
                query = query.Where(m => m.Homing == homing.Value);
            }

            var moves = await query
                .OrderBy(m => m.CharacterName)
                .ThenBy(m => m.Command)
                .ToListAsync();

            return Ok(moves);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error searching moves");
            return StatusCode(500, new { message = "Internal server error" });
        }
    }

    /// <summary>
    /// Получить количество движений
    /// </summary>
    [HttpGet("count")]
    [RequirePermission(RolePermissions.ViewMoves)]
    public async Task<ActionResult<int>> GetMoveCount()
    {
        try
        {
            var count = await context.TekkenMoves.CountAsync();
            return Ok(count);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error getting move count");
            return StatusCode(500, new { message = "Internal server error" });
        }
    }

    /// <summary>
    /// Получить статистику движений по персонажам
    /// </summary>
    [HttpGet("stats")]
    [RequirePermission(RolePermissions.ViewMoves)]
    public async Task<ActionResult<object>> GetMoveStats()
    {
        try
        {
            var stats = await context
                .TekkenMoves.GroupBy(m => m.CharacterName)
                .Select(g => new
                {
                    CharacterName = g.Key,
                    TotalMoves = g.Count(),
                    HeatEngageMoves = g.Count(m => m.HeatEngage),
                    PowerCrushMoves = g.Count(m => m.PowerCrush),
                    ThrowMoves = g.Count(m => m.Throw),
                    HomingMoves = g.Count(m => m.Homing),
                })
                .OrderBy(s => s.CharacterName)
                .ToListAsync();

            return Ok(stats);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error getting move stats");
            return StatusCode(500, new { message = "Internal server error" });
        }
    }
}

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

namespace TekkenFrameData.Service.API.v1.CharacterController;

[ApiController]
[Route("api/v1/[controller]")]
public class CharacterController(AppDbContext context, ILogger<CharacterController> logger)
    : ControllerBase
{
    /// <summary>
    /// Получить всех персонажей
    /// </summary>
    [HttpGet]
    [RequirePermission(RolePermissions.ViewCharacters)]
    public async Task<ActionResult<IEnumerable<Character>>> GetCharacters()
    {
        try
        {
            var characters = await context
                .TekkenCharacters.Include(c => c.Movelist)
                .OrderBy(c => c.Name)
                .ToListAsync();

            return Ok(characters);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error getting characters");
            return StatusCode(500, new { message = "Internal server error" });
        }
    }

    /// <summary>
    /// Получить персонажа по имени
    /// </summary>
    [HttpGet("{characterName}")]
    [RequirePermission(RolePermissions.ViewCharacters)]
    public async Task<ActionResult<Character>> GetCharacter(string characterName)
    {
        try
        {
            var character = await context
                .TekkenCharacters.Include(c => c.Movelist)
                .FirstOrDefaultAsync(c =>
                    c.Name.Equals(characterName, StringComparison.CurrentCultureIgnoreCase)
                );

            return character == null
                ? (ActionResult<Character>)NotFound(new { message = "Character not found" })
                : (ActionResult<Character>)Ok(character);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error getting character {CharacterName}", characterName);
            return StatusCode(500, new { message = "Internal server error" });
        }
    }

    /// <summary>
    /// Создать нового персонажа
    /// </summary>
    [HttpPost]
    [RequirePermission(RolePermissions.ManageCharacters)]
    public async Task<ActionResult<Character>> CreateCharacter([FromBody] Character character)
    {
        try
        {
            if (
                await context.TekkenCharacters.AnyAsync(c =>
                    c.Name.Equals(character.Name, StringComparison.CurrentCultureIgnoreCase)
                )
            )
            {
                return BadRequest(new { message = "Character with this name already exists" });
            }

            character.LastUpdateTime = DateTime.Now.ToLocalTime();
            context.TekkenCharacters.Add(character);
            await context.SaveChangesAsync();

            return CreatedAtAction(
                nameof(GetCharacter),
                new { characterName = character.Name },
                character
            );
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error creating character {CharacterName}", character.Name);
            return StatusCode(500, new { message = "Internal server error" });
        }
    }

    /// <summary>
    /// Обновить персонажа
    /// </summary>
    [HttpPut("{characterName}")]
    [RequirePermission(RolePermissions.ManageCharacters)]
    public async Task<IActionResult> UpdateCharacter(
        string characterName,
        [FromBody] Character character
    )
    {
        try
        {
            var existingCharacter = await context.TekkenCharacters.FirstOrDefaultAsync(c =>
                c.Name.Equals(characterName, StringComparison.CurrentCultureIgnoreCase)
            );

            if (existingCharacter == null)
            {
                return NotFound(new { message = "Character not found" });
            }

            // Обновляем только разрешенные поля
            existingCharacter.LinkToImage = character.LinkToImage;
            existingCharacter.Description = character.Description;
            existingCharacter.Strengths = character.Strengths;
            existingCharacter.Weaknesess = character.Weaknesess;
            existingCharacter.Image = character.Image;
            existingCharacter.ImageExtension = character.ImageExtension;
            existingCharacter.PageUrl = character.PageUrl;
            existingCharacter.LastUpdateTime = DateTime.Now.ToLocalTime();

            await context.SaveChangesAsync();

            return Ok(existingCharacter);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error updating character {CharacterName}", characterName);
            return StatusCode(500, new { message = "Internal server error" });
        }
    }

    /// <summary>
    /// Удалить персонажа
    /// </summary>
    [HttpDelete("{characterName}")]
    [RequirePermission(RolePermissions.ManageCharacters)]
    public async Task<IActionResult> DeleteCharacter(string characterName)
    {
        try
        {
            var character = await context
                .TekkenCharacters.Include(c => c.Movelist)
                .FirstOrDefaultAsync(c =>
                    c.Name.Equals(characterName, StringComparison.CurrentCultureIgnoreCase)
                );

            if (character == null)
            {
                return NotFound(new { message = "Character not found" });
            }

            // Удаляем все движения персонажа
            context.TekkenMoves.RemoveRange(character.Movelist);

            // Удаляем персонажа
            context.TekkenCharacters.Remove(character);
            await context.SaveChangesAsync();

            return Ok(new { message = "Character deleted successfully" });
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error deleting character {CharacterName}", characterName);
            return StatusCode(500, new { message = "Internal server error" });
        }
    }

    /// <summary>
    /// Получить количество персонажей
    /// </summary>
    [HttpGet("count")]
    [RequirePermission(RolePermissions.ViewCharacters)]
    public async Task<ActionResult<int>> GetCharacterCount()
    {
        try
        {
            var count = await context.TekkenCharacters.CountAsync();
            return Ok(count);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error getting character count");
            return StatusCode(500, new { message = "Internal server error" });
        }
    }
}

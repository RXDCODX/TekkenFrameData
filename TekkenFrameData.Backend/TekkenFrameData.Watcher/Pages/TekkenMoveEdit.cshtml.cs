using System;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using TekkenFrameData.Watcher.DB;
using TekkenFrameData.Watcher.Domains.FrameData;

namespace TekkenFrameData.Watcher.Pages;

public class TekkenMoveEditModel : PageModel
{
    private readonly AppDbContext _context;

    public TekkenMoveEditModel(AppDbContext context)
    {
        _context = context;
    }

    [BindProperty]
    public TekkenMove TekkenMove { get; set; } = default!;

    [BindProperty]
    public TekkenMove OriginalTekkenMove { get; set; } = default!;

    public async Task<ActionResult> OnGetAsync([FromRoute] string? charactername, [FromRoute] string? command)
    {
        if ((string.IsNullOrWhiteSpace(charactername) && string.IsNullOrWhiteSpace(command)) || _context.TekkenMoves == null)
        {
            return BadRequest("bad route");
        }

        var tekkenmove = await _context.TekkenMoves.FindAsync(charactername, command);
        if (tekkenmove == null)
        {
            return BadRequest("No tekken move");
        }

        TekkenMove = tekkenmove;
        OriginalTekkenMove = new TekkenMove
        {
            CharacterName = tekkenmove.CharacterName,
            Command = tekkenmove.Command,
            StanceCode = tekkenmove.StanceCode,
            StanceName = tekkenmove.StanceName,
            HitLevel = tekkenmove.HitLevel,
            Damage = tekkenmove.Damage,
            StartUpFrame = tekkenmove.StartUpFrame,
            BlockFrame = tekkenmove.BlockFrame,
            HitFrame = tekkenmove.HitFrame,
            CounterHitFrame = tekkenmove.CounterHitFrame,
            Notes = tekkenmove.Notes,
        };

        return Page();
    }

    public async Task<IActionResult> OnPostSaveAsync()
    {
        if (!ModelState.IsValid)
        {
            var message = string.Join(Environment.NewLine, ModelState.Values.Select(e => string.Join(Environment.NewLine + "  ", e.Errors.Select(r => r.ErrorMessage))));

            return BadRequest(message);
        }

        _context.Attach(TekkenMove).State = EntityState.Modified;

        try
        {
            await _context.SaveChangesAsync();
        }
        catch (DbUpdateConcurrencyException)
        {
            if (!TekkenMoveExists(TekkenMove.CharacterName, TekkenMove.Command))
            {
                return NotFound();
            }
            else
            {
                throw;
            }
        }

        return StatusCode(200, TekkenMove);
    }

    public IActionResult OnPostCancel()
    {
        // Восстановление оригинальных значений
        TekkenMove = OriginalTekkenMove;
        return Page();
    }

    private bool TekkenMoveExists(string charname, string command)
    {
        return _context.TekkenMoves?.Find(charname, command) != null;
    }
}
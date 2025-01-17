using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using tekkenfd.DB;
using tekkenfd.Domains.FrameData;

namespace tekkenfd.Pages;

public class ListChars : PageModel
{
    private readonly AppDbContext _context;

    public ListChars(AppDbContext context)
    {
        _context = context;
    }

    public class TekkenCharacterViewModel
    {
        public IEnumerable<TekkenCharacter> Characters { get; set; }
        public string SelectedCharacterName { get; set; }
        public IEnumerable<TekkenMove> SelectedCharacterMoves { get; set; } = Enumerable.Empty<TekkenMove>();
        public TekkenMove SelectedMove { get; set; }
    }

    [BindProperty]
    public TekkenCharacterViewModel ViewModel { get; set; }

    public void OnGet()
    {
        ViewModel = new TekkenCharacterViewModel
        {
            Characters = _context.TekkenCharacters.Include(c => c.Movelist).ToList()
        };
    }

    public IActionResult OnPost()
    {
        var selectedCharacterName = ViewModel.SelectedCharacterName;

        if (!string.IsNullOrEmpty(selectedCharacterName))
        {
            var selectedCharacter = _context.TekkenCharacters
                .Include(c => c.Movelist)
                .FirstOrDefault(c => c.Name == selectedCharacterName);

            ViewModel.SelectedCharacterMoves = selectedCharacter?.Movelist ?? Enumerable.Empty<TekkenMove>();
        }

        // Перезагружаем список персонажей для отображения в представлении
        ViewModel.Characters = _context.TekkenCharacters.Include(c => c.Movelist).ToList();

        return Page();
    }

    public IActionResult OnGetEdit(string character, string command)
    {
        return RedirectToPage("/Edit", new { character, command });
    }
}
using System.Security.Claims;
using BookClub.Data;
using BookClub.Models.Entities;
using BookClub.Models.Enums;
using BookClub.Models.ViewModels;
using BookClub.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace BookClub.Controllers;

[Authorize]
public class ClubBookController : Controller
{
    private readonly AppDbContext _context;
    private readonly IBookApiService _bookApiService;

    public ClubBookController(AppDbContext context, IBookApiService bookApiService)
    {
        _context = context;
        _bookApiService = bookApiService;
    }

    [HttpGet]
    public async Task<IActionResult> SearchApi(string? query, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(query) || query.Trim().Length < 2)
        {
            return Json(new
            {
                results = Array.Empty<BookSearchResultDto>(),
                warning = "Digite pelo menos 2 caracteres para buscar um livro."
            });
        }

        var (results, warning) = await _bookApiService.SearchBooksAsync(query, cancellationToken);
        return Json(new { results, warning });
    }

    [HttpGet]
    public async Task<IActionResult> Add(int clubId)
    {
        var club = await GetOwnedClubAsync(clubId);
        if (club is null)
        {
            TempData["ErrorMessage"] = "Apenas o administrador do clube pode adicionar livros.";
            return RedirectToAction("Details", "Club", new { id = clubId });
        }

        var hasCurrent = await _context.ClubBooks
            .AnyAsync(cb => cb.ClubId == clubId && cb.Status == ClubBookStatus.Current);

        var nextOrder = await _context.ClubBooks
            .Where(cb => cb.ClubId == clubId && cb.Status == ClubBookStatus.Upcoming)
            .Select(cb => (int?)cb.DisplayOrder)
            .MaxAsync() ?? 0;

        var model = new AddClubBookViewModel
        {
            ClubId = club.Id,
            ClubName = club.Name,
            Status = hasCurrent ? ClubBookStatus.Upcoming : ClubBookStatus.Current,
            DisplayOrder = nextOrder + 1
        };

        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Add(AddClubBookViewModel model)
    {
        var club = await GetOwnedClubAsync(model.ClubId);
        if (club is null)
        {
            TempData["ErrorMessage"] = "Apenas o administrador do clube pode adicionar livros.";
            return RedirectToAction("Index", "Club");
        }

        model.ClubName = club.Name;

        if (model.StartDate.HasValue && model.TargetFinishDate.HasValue &&
            model.TargetFinishDate.Value < model.StartDate.Value)
        {
            ModelState.AddModelError(nameof(model.TargetFinishDate),
                "A data prevista de término deve ser igual ou posterior à data de início.");
        }

        if (!ModelState.IsValid)
        {
            return View(model);
        }

        var sanitizedExternalId = CleanOptional(model.ExternalId, 100);
        var sanitizedIsbn = CleanOptional(model.Isbn, 20);
        var sanitizedCoverUrl = SanitizeHttpUrl(model.CoverUrl, 500);

        Book? book = null;
        if (!string.IsNullOrWhiteSpace(sanitizedExternalId))
        {
            book = await _context.Books.FirstOrDefaultAsync(b => b.ExternalId == sanitizedExternalId);
        }

        if (book is null && !string.IsNullOrWhiteSpace(sanitizedIsbn))
        {
            book = await _context.Books.FirstOrDefaultAsync(b => b.Isbn == sanitizedIsbn);
        }

        if (book is null)
        {
            book = new Book
            {
                ExternalId = sanitizedExternalId,
                Isbn = sanitizedIsbn,
                Name = model.Name.Trim(),
                Author = model.Author.Trim(),
                Synopsis = string.IsNullOrWhiteSpace(model.Synopsis) ? null : model.Synopsis.Trim(),
                Pages = model.Pages,
                CoverUrl = sanitizedCoverUrl,
                PublishedYear = model.PublishedYear
            };

            _context.Books.Add(book);
            await _context.SaveChangesAsync();
        }

        var startDate = model.StartDate;
        if (model.Status == ClubBookStatus.Current && !startDate.HasValue)
        {
            startDate = DateOnly.FromDateTime(DateTime.Today);
        }

        var clubBook = new ClubBook
        {
            ClubId = club.Id,
            BookId = book.Id,
            Status = model.Status,
            StartDate = startDate,
            TargetFinishDate = model.TargetFinishDate,
            DisplayOrder = model.DisplayOrder
        };

        _context.ClubBooks.Add(clubBook);
        await _context.SaveChangesAsync();

        TempData["SuccessMessage"] = $"Livro \"{book.Name}\" adicionado ao cronograma do clube!";
        return RedirectToAction("Details", "Club", new { id = club.Id, tab = StatusToTab(model.Status) });
    }

    [HttpGet]
    public async Task<IActionResult> Edit(int id)
    {
        var userId = GetCurrentUserId();
        var clubBook = await _context.ClubBooks
            .AsNoTracking()
            .Include(cb => cb.Book)
            .Include(cb => cb.Club)
            .FirstOrDefaultAsync(cb => cb.Id == id);

        if (clubBook is null)
        {
            TempData["ErrorMessage"] = "Livro não encontrado no cronograma do clube.";
            return RedirectToAction("Index", "Club");
        }

        var isOwner = await _context.UserClubs
            .AnyAsync(uc => uc.ClubId == clubBook.ClubId && uc.UserId == userId && uc.Role == ClubRole.Owner && uc.Status == MembershipStatus.Active);

        if (!isOwner)
        {
            TempData["ErrorMessage"] = "Apenas o administrador do clube pode editar livros do cronograma.";
            return RedirectToAction("Details", "Club", new { id = clubBook.ClubId });
        }

        var model = new EditClubBookViewModel
        {
            Id = clubBook.Id,
            ClubId = clubBook.ClubId,
            ClubName = clubBook.Club.Name,
            BookTitle = clubBook.Book.Name,
            BookAuthor = clubBook.Book.Author,
            BookCoverUrl = clubBook.Book.CoverUrl,
            Status = clubBook.Status,
            StartDate = clubBook.StartDate,
            TargetFinishDate = clubBook.TargetFinishDate,
            DisplayOrder = clubBook.DisplayOrder
        };

        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, EditClubBookViewModel model)
    {
        var userId = GetCurrentUserId();
        var clubBook = await _context.ClubBooks
            .Include(cb => cb.Book)
            .Include(cb => cb.Club)
            .FirstOrDefaultAsync(cb => cb.Id == id);

        if (clubBook is null)
        {
            TempData["ErrorMessage"] = "Livro não encontrado no cronograma do clube.";
            return RedirectToAction("Index", "Club");
        }

        var isOwner = await _context.UserClubs
            .AnyAsync(uc => uc.ClubId == clubBook.ClubId && uc.UserId == userId && uc.Role == ClubRole.Owner && uc.Status == MembershipStatus.Active);

        if (!isOwner)
        {
            TempData["ErrorMessage"] = "Apenas o administrador do clube pode editar livros do cronograma.";
            return RedirectToAction("Details", "Club", new { id = clubBook.ClubId });
        }

        model.Id = clubBook.Id;
        model.ClubId = clubBook.ClubId;
        model.ClubName = clubBook.Club.Name;
        model.BookTitle = clubBook.Book.Name;
        model.BookAuthor = clubBook.Book.Author;
        model.BookCoverUrl = clubBook.Book.CoverUrl;

        if (model.StartDate.HasValue && model.TargetFinishDate.HasValue &&
            model.TargetFinishDate.Value < model.StartDate.Value)
        {
            ModelState.AddModelError(nameof(model.TargetFinishDate),
                "A data prevista de término deve ser igual ou posterior à data de início.");
        }

        if (!ModelState.IsValid)
        {
            return View(model);
        }

        clubBook.Status = model.Status;
        clubBook.StartDate = model.StartDate;
        clubBook.TargetFinishDate = model.TargetFinishDate;
        clubBook.DisplayOrder = model.DisplayOrder;

        await _context.SaveChangesAsync();

        TempData["SuccessMessage"] = $"Cronograma do livro \"{clubBook.Book.Name}\" atualizado!";
        return RedirectToAction("Details", "Club", new { id = clubBook.ClubId, tab = StatusToTab(clubBook.Status) });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> UpdateStatus(int id, ClubBookStatus status)
    {
        var userId = GetCurrentUserId();
        var clubBook = await _context.ClubBooks
            .Include(cb => cb.Book)
            .FirstOrDefaultAsync(cb => cb.Id == id);

        if (clubBook is null)
        {
            TempData["ErrorMessage"] = "Livro não encontrado no clube.";
            return RedirectToAction("Index", "Club");
        }

        var isOwner = await _context.UserClubs
            .AnyAsync(uc => uc.ClubId == clubBook.ClubId && uc.UserId == userId && uc.Role == ClubRole.Owner && uc.Status == MembershipStatus.Active);

        if (!isOwner)
        {
            TempData["ErrorMessage"] = "Apenas o administrador do clube pode alterar o status das leituras.";
            return RedirectToAction("Details", "Club", new { id = clubBook.ClubId });
        }

        clubBook.Status = status;

        if (status == ClubBookStatus.Current && !clubBook.StartDate.HasValue)
        {
            clubBook.StartDate = DateOnly.FromDateTime(DateTime.Today);
        }
        else if (status == ClubBookStatus.Finished && !clubBook.TargetFinishDate.HasValue)
        {
            clubBook.TargetFinishDate = DateOnly.FromDateTime(DateTime.Today);
        }

        await _context.SaveChangesAsync();

        var statusLabel = status switch
        {
            ClubBookStatus.Current => "Leitura Atual",
            ClubBookStatus.Upcoming => "Próximas Leituras",
            ClubBookStatus.Finished => "Já Lidos",
            _ => "atualizado"
        };

        TempData["SuccessMessage"] = $"\"{clubBook.Book.Name}\" movido para {statusLabel}.";
        return RedirectToAction("Details", "Club", new { id = clubBook.ClubId, tab = StatusToTab(status) });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Remove(int id)
    {
        var userId = GetCurrentUserId();
        var clubBook = await _context.ClubBooks
            .Include(cb => cb.Book)
            .FirstOrDefaultAsync(cb => cb.Id == id);

        if (clubBook is null)
        {
            TempData["ErrorMessage"] = "Livro não encontrado no clube.";
            return RedirectToAction("Index", "Club");
        }

        var isOwner = await _context.UserClubs
            .AnyAsync(uc => uc.ClubId == clubBook.ClubId && uc.UserId == userId && uc.Role == ClubRole.Owner && uc.Status == MembershipStatus.Active);

        if (!isOwner)
        {
            TempData["ErrorMessage"] = "Apenas o administrador do clube pode remover livros.";
            return RedirectToAction("Details", "Club", new { id = clubBook.ClubId });
        }

        var clubId = clubBook.ClubId;
        var bookTitle = clubBook.Book.Name;
        var tab = StatusToTab(clubBook.Status);

        _context.ClubBooks.Remove(clubBook);
        await _context.SaveChangesAsync();

        TempData["InfoMessage"] = $"Livro \"{bookTitle}\" removido do cronograma do clube.";
        return RedirectToAction("Details", "Club", new { id = clubId, tab });
    }

    private async Task<Club?> GetOwnedClubAsync(int clubId)
    {
        var userId = GetCurrentUserId();
        return await _context.Clubs
            .AsNoTracking()
            .FirstOrDefaultAsync(c => c.Id == clubId && c.UserClubs.Any(uc => uc.UserId == userId && uc.Role == ClubRole.Owner && uc.Status == MembershipStatus.Active));
    }

    private int GetCurrentUserId()
    {
        var claim = User.FindFirstValue(ClaimTypes.NameIdentifier);
        return int.TryParse(claim, out var id) ? id : 0;
    }

    private static string StatusToTab(ClubBookStatus status) => status switch
    {
        ClubBookStatus.Current => "current",
        ClubBookStatus.Upcoming => "upcoming",
        ClubBookStatus.Finished => "finished",
        _ => "current"
    };

    private static string? CleanOptional(string? value, int maxLength)
    {
        if (string.IsNullOrWhiteSpace(value)) return null;
        var trimmed = value.Trim();
        return trimmed.Length <= maxLength ? trimmed : trimmed[..maxLength];
    }

    private static string? SanitizeHttpUrl(string? rawUrl, int maxLength)
    {
        if (string.IsNullOrWhiteSpace(rawUrl)) return null;
        var trimmed = rawUrl.Trim();
        if (!Uri.TryCreate(trimmed, UriKind.Absolute, out var uri)) return null;
        if (uri.Scheme != Uri.UriSchemeHttp && uri.Scheme != Uri.UriSchemeHttps) return null;
        var normalized = uri.Scheme == Uri.UriSchemeHttp
            ? "https://" + trimmed[7..]
            : trimmed;
        return normalized.Length <= maxLength ? normalized : normalized[..maxLength];
    }
}

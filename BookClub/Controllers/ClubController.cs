using System.Security.Claims;
using System.Security.Cryptography;
using BookClub.Data;
using BookClub.Models.Entities;
using BookClub.Models.Enums;
using BookClub.Models.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace BookClub.Controllers;

[Authorize]
public class ClubController : Controller
{
    private const string InviteAlphabet = "ABCDEFGHJKLMNPQRSTUVWXYZ23456789";
    private readonly AppDbContext _context;

    public ClubController(AppDbContext context)
    {
        _context = context;
    }

    [HttpGet]
    public async Task<IActionResult> Index()
    {
        var userId = GetCurrentUserId();

        var clubs = await _context.UserClubs
            .AsNoTracking()
            .Where(uc => uc.UserId == userId && uc.Status == MembershipStatus.Active)
            .OrderByDescending(uc => uc.Role == ClubRole.Owner)
            .ThenByDescending(uc => uc.CreatedAt)
            .Select(uc => new ClubSummaryItemViewModel
            {
                Id = uc.ClubId,
                Name = uc.Club.Name,
                Description = uc.Club.Description,
                IsPrivate = uc.Club.IsPrivate,
                MemberCount = uc.Club.UserClubs.Count(m => m.Status == MembershipStatus.Active),
                PendingRequestsCount = uc.Role == ClubRole.Owner
                    ? uc.Club.UserClubs.Count(m => m.Status == MembershipStatus.Pending)
                    : 0,
                CurrentBookTitle = uc.Club.ClubBooks
                    .Where(cb => cb.Status == ClubBookStatus.Current)
                    .OrderBy(cb => cb.DisplayOrder)
                    .ThenBy(cb => cb.CreatedAt)
                    .Select(cb => cb.Book.Name)
                    .FirstOrDefault(),
                CurrentBookCoverUrl = uc.Club.ClubBooks
                    .Where(cb => cb.Status == ClubBookStatus.Current)
                    .OrderBy(cb => cb.DisplayOrder)
                    .ThenBy(cb => cb.CreatedAt)
                    .Select(cb => cb.Book.CoverUrl)
                    .FirstOrDefault(),
                UserRole = uc.Role,
                MembershipStatus = uc.Status
            })
            .ToListAsync();

        var pendingClubs = await _context.UserClubs
            .AsNoTracking()
            .Where(uc => uc.UserId == userId && uc.Status == MembershipStatus.Pending)
            .OrderByDescending(uc => uc.CreatedAt)
            .Select(uc => new ClubSummaryItemViewModel
            {
                Id = uc.ClubId,
                Name = uc.Club.Name,
                Description = uc.Club.Description,
                IsPrivate = uc.Club.IsPrivate,
                MemberCount = uc.Club.UserClubs.Count(m => m.Status == MembershipStatus.Active),
                UserRole = uc.Role,
                MembershipStatus = uc.Status
            })
            .ToListAsync();

        return View(new MyClubsViewModel
        {
            Clubs = clubs,
            PendingClubs = pendingClubs
        });
    }

    [HttpGet]
    public async Task<IActionResult> Explore(string? search)
    {
        var userId = GetCurrentUserId();
        var trimmedSearch = search?.Trim();

        var query = _context.Clubs
            .AsNoTracking()
            .Where(c => !c.IsPrivate);

        if (!string.IsNullOrWhiteSpace(trimmedSearch))
        {
            var pattern = $"%{trimmedSearch}%";
            query = query.Where(c =>
                EF.Functions.ILike(c.Name, pattern) ||
                (c.Description != null && EF.Functions.ILike(c.Description, pattern)));
        }

        var publicClubs = await query
            .OrderByDescending(c => c.UserClubs.Count(uc => uc.Status == MembershipStatus.Active))
            .ThenBy(c => c.Name)
            .Select(c => new ClubSummaryItemViewModel
            {
                Id = c.Id,
                Name = c.Name,
                Description = c.Description,
                IsPrivate = c.IsPrivate,
                MemberCount = c.UserClubs.Count(uc => uc.Status == MembershipStatus.Active),
                CurrentBookTitle = c.ClubBooks
                    .Where(cb => cb.Status == ClubBookStatus.Current)
                    .OrderBy(cb => cb.DisplayOrder)
                    .Select(cb => cb.Book.Name)
                    .FirstOrDefault(),
                CurrentBookCoverUrl = c.ClubBooks
                    .Where(cb => cb.Status == ClubBookStatus.Current)
                    .OrderBy(cb => cb.DisplayOrder)
                    .Select(cb => cb.Book.CoverUrl)
                    .FirstOrDefault(),
                UserRole = c.UserClubs
                    .Where(uc => uc.UserId == userId)
                    .Select(uc => (ClubRole?)uc.Role)
                    .FirstOrDefault(),
                MembershipStatus = c.UserClubs
                    .Where(uc => uc.UserId == userId)
                    .Select(uc => (MembershipStatus?)uc.Status)
                    .FirstOrDefault()
            })
            .ToListAsync();

        return View(new ExploreClubsViewModel
        {
            Search = trimmedSearch,
            PublicClubs = publicClubs
        });
    }

    [HttpGet]
    public IActionResult Create()
    {
        return View(new ClubFormViewModel());
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(ClubFormViewModel model)
    {
        if (!ModelState.IsValid)
        {
            return View(model);
        }

        var userId = GetCurrentUserId();
        var inviteCode = await GenerateUniqueInviteCodeAsync();

        var club = new Club
        {
            Name = model.Name.Trim(),
            Description = string.IsNullOrWhiteSpace(model.Description) ? null : model.Description.Trim(),
            IsPrivate = model.IsPrivate,
            InviteCode = inviteCode
        };

        club.UserClubs.Add(new UserClub
        {
            UserId = userId,
            Role = ClubRole.Owner,
            Status = MembershipStatus.Active
        });

        _context.Clubs.Add(club);
        await _context.SaveChangesAsync();

        TempData["SuccessMessage"] = $"Clube \"{club.Name}\" criado com sucesso! Código de convite: {club.InviteCode}";
        return RedirectToAction(nameof(Details), new { id = club.Id });
    }

    [HttpGet]
    public async Task<IActionResult> Details(int id, string? tab = null)
    {
        var userId = GetCurrentUserId();

        var club = await _context.Clubs
            .AsNoTracking()
            .Include(c => c.UserClubs)
                .ThenInclude(uc => uc.User)
            .Include(c => c.ClubBooks)
                .ThenInclude(cb => cb.Book)
            .FirstOrDefaultAsync(c => c.Id == id);

        if (club is null)
        {
            TempData["ErrorMessage"] = "Clube não encontrado.";
            return RedirectToAction(nameof(Index));
        }

        var userMembership = club.UserClubs.FirstOrDefault(uc => uc.UserId == userId);

        if (userMembership?.Status == MembershipStatus.Banned)
        {
            TempData["ErrorMessage"] = "Você não tem permissão para acessar este clube.";
            return RedirectToAction(nameof(Index));
        }

        if (club.IsPrivate && userMembership?.Status != MembershipStatus.Active)
        {
            TempData["ErrorMessage"] = "Este clube é privado. Peça o código de convite ao administrador para entrar.";
            return RedirectToAction(nameof(Index));
        }

        var isOwner = userMembership?.Status == MembershipStatus.Active && userMembership.Role == ClubRole.Owner;

        var mapBook = (ClubBook cb) => new ClubBookItemViewModel
        {
            ClubBookId = cb.Id,
            BookId = cb.BookId,
            Title = cb.Book.Name,
            Author = cb.Book.Author,
            Synopsis = cb.Book.Synopsis,
            Pages = cb.Book.Pages,
            CoverUrl = cb.Book.CoverUrl,
            PublishedYear = cb.Book.PublishedYear,
            Isbn = cb.Book.Isbn,
            Status = cb.Status,
            StartDate = cb.StartDate,
            TargetFinishDate = cb.TargetFinishDate,
            DisplayOrder = cb.DisplayOrder
        };

        var mapMember = (UserClub uc) => new ClubMemberItemViewModel
        {
            UserId = uc.UserId,
            FullName = uc.User.FullName,
            Initials = BuildInitials(uc.User.Name, uc.User.Surname),
            Role = uc.Role,
            Status = uc.Status,
            JoinedAt = uc.UpdatedAt ?? uc.CreatedAt
        };

        var currentBooks = club.ClubBooks
            .Where(cb => cb.Status == ClubBookStatus.Current)
            .OrderBy(cb => cb.DisplayOrder)
            .ThenByDescending(cb => cb.CreatedAt)
            .Select(mapBook)
            .ToList();

        var upcomingBooks = club.ClubBooks
            .Where(cb => cb.Status == ClubBookStatus.Upcoming)
            .OrderBy(cb => cb.DisplayOrder)
            .ThenBy(cb => cb.CreatedAt)
            .Select(mapBook)
            .ToList();

        var finishedBooks = club.ClubBooks
            .Where(cb => cb.Status == ClubBookStatus.Finished)
            .OrderByDescending(cb => cb.TargetFinishDate)
            .ThenByDescending(cb => cb.UpdatedAt ?? cb.CreatedAt)
            .Select(mapBook)
            .ToList();

        var members = club.UserClubs
            .Where(uc => uc.Status == MembershipStatus.Active)
            .OrderByDescending(uc => uc.Role == ClubRole.Owner)
            .ThenBy(uc => uc.User.Name)
            .ThenBy(uc => uc.User.Surname)
            .Select(mapMember)
            .ToList();

        var pendingRequests = isOwner
            ? club.UserClubs
                .Where(uc => uc.Status == MembershipStatus.Pending)
                .OrderBy(uc => uc.CreatedAt)
                .Select(mapMember)
                .ToList()
            : new List<ClubMemberItemViewModel>();

        var bannedMembers = isOwner
            ? club.UserClubs
                .Where(uc => uc.Status == MembershipStatus.Banned)
                .OrderByDescending(uc => uc.UpdatedAt ?? uc.CreatedAt)
                .Select(mapMember)
                .ToList()
            : new List<ClubMemberItemViewModel>();

        var validTabs = new[] { "current", "upcoming", "finished", "members" };
        var activeTab = !string.IsNullOrWhiteSpace(tab) && validTabs.Contains(tab.ToLowerInvariant())
            ? tab.ToLowerInvariant()
            : "current";

        var viewModel = new ClubDetailsViewModel
        {
            Id = club.Id,
            Name = club.Name,
            Description = club.Description,
            IsPrivate = club.IsPrivate,
            InviteCode = club.InviteCode,
            CurrentUserRole = userMembership?.Role,
            CurrentUserStatus = userMembership?.Status,
            ActiveTab = activeTab,
            CurrentBooks = currentBooks,
            UpcomingBooks = upcomingBooks,
            FinishedBooks = finishedBooks,
            Members = members,
            PendingRequests = pendingRequests,
            BannedMembers = bannedMembers
        };

        return View(viewModel);
    }

    [HttpGet]
    public async Task<IActionResult> Edit(int id)
    {
        var userId = GetCurrentUserId();
        var club = await _context.Clubs
            .AsNoTracking()
            .FirstOrDefaultAsync(c => c.Id == id && c.UserClubs.Any(uc => uc.UserId == userId && uc.Role == ClubRole.Owner && uc.Status == MembershipStatus.Active));

        if (club is null)
        {
            TempData["ErrorMessage"] = "Apenas o administrador do clube pode editar suas informações.";
            return RedirectToAction(nameof(Index));
        }

        return View(new ClubFormViewModel
        {
            Id = club.Id,
            Name = club.Name,
            Description = club.Description,
            IsPrivate = club.IsPrivate
        });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, ClubFormViewModel model)
    {
        if (!ModelState.IsValid)
        {
            model.Id = id;
            return View(model);
        }

        if (!await IsClubOwnerAsync(id))
        {
            TempData["ErrorMessage"] = "Apenas o administrador do clube pode editar suas informações.";
            return RedirectToAction(nameof(Index));
        }

        var club = await _context.Clubs.FindAsync(id);
        if (club is null)
        {
            TempData["ErrorMessage"] = "Clube não encontrado.";
            return RedirectToAction(nameof(Index));
        }

        club.Name = model.Name.Trim();
        club.Description = string.IsNullOrWhiteSpace(model.Description) ? null : model.Description.Trim();
        club.IsPrivate = model.IsPrivate;

        await _context.SaveChangesAsync();

        TempData["SuccessMessage"] = "Dados do clube atualizados com sucesso.";
        return RedirectToAction(nameof(Details), new { id = club.Id });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Join(int id)
    {
        var userId = GetCurrentUserId();
        var club = await _context.Clubs.AsNoTracking().FirstOrDefaultAsync(c => c.Id == id);

        if (club is null)
        {
            TempData["ErrorMessage"] = "Clube não encontrado.";
            return RedirectToAction(nameof(Explore));
        }

        if (club.IsPrivate)
        {
            TempData["ErrorMessage"] = "Este clube é privado. Utilize o código de convite para participar.";
            return RedirectToAction(nameof(Index));
        }

        var existing = await _context.UserClubs
            .FirstOrDefaultAsync(uc => uc.ClubId == id && uc.UserId == userId);

        if (existing is not null)
        {
            if (existing.Status == MembershipStatus.Banned)
            {
                TempData["ErrorMessage"] = "Você não tem permissão para solicitar entrada neste clube.";
                return RedirectToAction(nameof(Explore));
            }

            if (existing.Status == MembershipStatus.Active)
            {
                TempData["InfoMessage"] = $"Você já faz parte do clube \"{club.Name}\".";
                return RedirectToAction(nameof(Details), new { id });
            }

            TempData["InfoMessage"] = $"Sua solicitação para o clube \"{club.Name}\" já foi enviada e aguarda aprovação do administrador.";
            return RedirectToAction(nameof(Explore));
        }

        _context.UserClubs.Add(new UserClub
        {
            UserId = userId,
            ClubId = id,
            Role = ClubRole.Member,
            Status = MembershipStatus.Pending
        });
        await _context.SaveChangesAsync();

        TempData["SuccessMessage"] = $"Solicitação enviada para \"{club.Name}\"! Aguarde a aprovação do administrador.";
        return RedirectToAction(nameof(Explore));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> CancelRequest(int id)
    {
        var userId = GetCurrentUserId();
        var pending = await _context.UserClubs
            .Include(uc => uc.Club)
            .FirstOrDefaultAsync(uc => uc.ClubId == id && uc.UserId == userId && uc.Status == MembershipStatus.Pending);

        if (pending is null)
        {
            TempData["ErrorMessage"] = "Solicitação não encontrada.";
            return RedirectToAction(nameof(Index));
        }

        var clubName = pending.Club.Name;
        _context.UserClubs.Remove(pending);
        await _context.SaveChangesAsync();

        TempData["InfoMessage"] = $"Solicitação de entrada para \"{clubName}\" cancelada.";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> JoinByCode(string? inviteCode)
    {
        if (string.IsNullOrWhiteSpace(inviteCode))
        {
            TempData["ErrorMessage"] = "Digite um código de convite para entrar em um clube.";
            return RedirectToAction(nameof(Index));
        }

        var normalizedCode = inviteCode.Trim().ToUpperInvariant();
        var club = await _context.Clubs
            .AsNoTracking()
            .FirstOrDefaultAsync(c => c.InviteCode == normalizedCode);

        if (club is null)
        {
            TempData["ErrorMessage"] = $"Nenhum clube encontrado com o código \"{normalizedCode}\". Verifique e tente novamente.";
            return RedirectToAction(nameof(Index));
        }

        var userId = GetCurrentUserId();
        var existing = await _context.UserClubs
            .FirstOrDefaultAsync(uc => uc.ClubId == club.Id && uc.UserId == userId);

        if (existing is not null)
        {
            if (existing.Status == MembershipStatus.Banned)
            {
                TempData["ErrorMessage"] = "Você não tem permissão para participar deste clube.";
                return RedirectToAction(nameof(Index));
            }

            if (existing.Status == MembershipStatus.Active)
            {
                TempData["InfoMessage"] = $"Você já faz parte do clube \"{club.Name}\".";
                return RedirectToAction(nameof(Details), new { id = club.Id });
            }

            existing.Status = MembershipStatus.Active;
            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = $"Você entrou no clube \"{club.Name}\" via código de convite!";
            return RedirectToAction(nameof(Details), new { id = club.Id });
        }

        _context.UserClubs.Add(new UserClub
        {
            UserId = userId,
            ClubId = club.Id,
            Role = ClubRole.Member,
            Status = MembershipStatus.Active
        });
        await _context.SaveChangesAsync();

        TempData["SuccessMessage"] = $"Você entrou no clube \"{club.Name}\" via código de convite!";
        return RedirectToAction(nameof(Details), new { id = club.Id });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ApproveMember(int clubId, int userId)
    {
        if (!await IsClubOwnerAsync(clubId))
        {
            TempData["ErrorMessage"] = "Apenas o administrador do clube pode aprovar solicitações.";
            return RedirectToAction(nameof(Details), new { id = clubId });
        }

        var membership = await _context.UserClubs
            .Include(uc => uc.User)
            .FirstOrDefaultAsync(uc => uc.ClubId == clubId && uc.UserId == userId && uc.Status == MembershipStatus.Pending);

        if (membership is null)
        {
            TempData["ErrorMessage"] = "Solicitação pendente não encontrada.";
            return RedirectToAction(nameof(Details), new { id = clubId, tab = "members" });
        }

        membership.Status = MembershipStatus.Active;
        await _context.SaveChangesAsync();

        TempData["SuccessMessage"] = $"{membership.User.FullName} foi aprovado(a) e agora faz parte do clube.";
        return RedirectToAction(nameof(Details), new { id = clubId, tab = "members" });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> RejectMember(int clubId, int userId)
    {
        if (!await IsClubOwnerAsync(clubId))
        {
            TempData["ErrorMessage"] = "Apenas o administrador do clube pode recusar solicitações.";
            return RedirectToAction(nameof(Details), new { id = clubId });
        }

        var membership = await _context.UserClubs
            .Include(uc => uc.User)
            .FirstOrDefaultAsync(uc => uc.ClubId == clubId && uc.UserId == userId && uc.Status == MembershipStatus.Pending);

        if (membership is null)
        {
            TempData["ErrorMessage"] = "Solicitação pendente não encontrada.";
            return RedirectToAction(nameof(Details), new { id = clubId, tab = "members" });
        }

        var userName = membership.User.FullName;
        _context.UserClubs.Remove(membership);
        await _context.SaveChangesAsync();

        TempData["InfoMessage"] = $"Solicitação de {userName} recusada.";
        return RedirectToAction(nameof(Details), new { id = clubId, tab = "members" });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> RemoveMember(int clubId, int userId)
    {
        if (!await IsClubOwnerAsync(clubId))
        {
            TempData["ErrorMessage"] = "Apenas o administrador do clube pode remover membros.";
            return RedirectToAction(nameof(Details), new { id = clubId });
        }

        if (userId == GetCurrentUserId())
        {
            TempData["ErrorMessage"] = "Você não pode remover a si mesmo como administrador.";
            return RedirectToAction(nameof(Details), new { id = clubId, tab = "members" });
        }

        var membership = await _context.UserClubs
            .Include(uc => uc.User)
            .FirstOrDefaultAsync(uc => uc.ClubId == clubId && uc.UserId == userId && uc.Status == MembershipStatus.Active);

        if (membership is null || membership.Role == ClubRole.Owner)
        {
            TempData["ErrorMessage"] = "Membro não encontrado.";
            return RedirectToAction(nameof(Details), new { id = clubId, tab = "members" });
        }

        var userName = membership.User.FullName;
        _context.UserClubs.Remove(membership);
        await _context.SaveChangesAsync();

        TempData["InfoMessage"] = $"{userName} foi removido(a) do clube.";
        return RedirectToAction(nameof(Details), new { id = clubId, tab = "members" });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> BanMember(int clubId, int userId)
    {
        if (!await IsClubOwnerAsync(clubId))
        {
            TempData["ErrorMessage"] = "Apenas o administrador do clube pode banir usuários.";
            return RedirectToAction(nameof(Details), new { id = clubId });
        }

        if (userId == GetCurrentUserId())
        {
            TempData["ErrorMessage"] = "Você não pode banir a si mesmo.";
            return RedirectToAction(nameof(Details), new { id = clubId, tab = "members" });
        }

        var membership = await _context.UserClubs
            .Include(uc => uc.User)
            .FirstOrDefaultAsync(uc => uc.ClubId == clubId && uc.UserId == userId);

        if (membership is null || membership.Role == ClubRole.Owner)
        {
            TempData["ErrorMessage"] = "Usuário não encontrado neste clube.";
            return RedirectToAction(nameof(Details), new { id = clubId, tab = "members" });
        }

        membership.Status = MembershipStatus.Banned;
        await _context.SaveChangesAsync();

        TempData["InfoMessage"] = $"{membership.User.FullName} foi banido(a) e não poderá mais solicitar entrada neste clube.";
        return RedirectToAction(nameof(Details), new { id = clubId, tab = "members" });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> UnbanMember(int clubId, int userId)
    {
        if (!await IsClubOwnerAsync(clubId))
        {
            TempData["ErrorMessage"] = "Apenas o administrador do clube pode desbanir usuários.";
            return RedirectToAction(nameof(Details), new { id = clubId });
        }

        var membership = await _context.UserClubs
            .Include(uc => uc.User)
            .FirstOrDefaultAsync(uc => uc.ClubId == clubId && uc.UserId == userId && uc.Status == MembershipStatus.Banned);

        if (membership is null)
        {
            TempData["ErrorMessage"] = "Usuário banido não encontrado.";
            return RedirectToAction(nameof(Details), new { id = clubId, tab = "members" });
        }

        var userName = membership.User.FullName;
        _context.UserClubs.Remove(membership);
        await _context.SaveChangesAsync();

        TempData["SuccessMessage"] = $"Banimento de {userName} removido.";
        return RedirectToAction(nameof(Details), new { id = clubId, tab = "members" });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Leave(int id)
    {
        var userId = GetCurrentUserId();
        var membership = await _context.UserClubs
            .Include(uc => uc.Club)
            .FirstOrDefaultAsync(uc => uc.ClubId == id && uc.UserId == userId && uc.Status == MembershipStatus.Active);

        if (membership is null)
        {
            TempData["ErrorMessage"] = "Você não é membro deste clube.";
            return RedirectToAction(nameof(Index));
        }

        if (membership.Role == ClubRole.Owner)
        {
            TempData["ErrorMessage"] = "O administrador não pode sair do próprio clube. Caso deseje encerrá-lo, utilize a opção \"Excluir Clube\".";
            return RedirectToAction(nameof(Details), new { id });
        }

        var clubName = membership.Club.Name;
        _context.UserClubs.Remove(membership);
        await _context.SaveChangesAsync();

        TempData["InfoMessage"] = $"Você saiu do clube \"{clubName}\".";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(int id)
    {
        if (!await IsClubOwnerAsync(id))
        {
            TempData["ErrorMessage"] = "Apenas o administrador pode excluir o clube.";
            return RedirectToAction(nameof(Index));
        }

        var club = await _context.Clubs.FindAsync(id);
        if (club is null)
        {
            TempData["ErrorMessage"] = "Clube não encontrado.";
            return RedirectToAction(nameof(Index));
        }

        var clubName = club.Name;
        _context.Clubs.Remove(club);
        await _context.SaveChangesAsync();

        TempData["SuccessMessage"] = $"Clube \"{clubName}\" excluído com sucesso.";
        return RedirectToAction(nameof(Index));
    }

    private Task<bool> IsClubOwnerAsync(int clubId)
    {
        var userId = GetCurrentUserId();
        return _context.UserClubs.AnyAsync(uc =>
            uc.ClubId == clubId &&
            uc.UserId == userId &&
            uc.Role == ClubRole.Owner &&
            uc.Status == MembershipStatus.Active);
    }

    private async Task<string> GenerateUniqueInviteCodeAsync()
    {
        for (var attempt = 0; attempt < 20; attempt++)
        {
            var chars = new char[8];
            chars[0] = 'C';
            chars[1] = 'L';
            for (var i = 2; i < chars.Length; i++)
            {
                chars[i] = InviteAlphabet[RandomNumberGenerator.GetInt32(InviteAlphabet.Length)];
            }

            var candidate = new string(chars);
            var exists = await _context.Clubs.AnyAsync(c => c.InviteCode == candidate);
            if (!exists)
            {
                return candidate;
            }
        }

        return $"CL{Guid.NewGuid().ToString("N")[..6].ToUpperInvariant()}";
    }

    private static string BuildInitials(string name, string surname)
    {
        var first = string.IsNullOrWhiteSpace(name) ? "" : name.Trim()[0].ToString().ToUpperInvariant();
        var second = string.IsNullOrWhiteSpace(surname) ? "" : surname.Trim()[0].ToString().ToUpperInvariant();
        return $"{first}{second}";
    }

    private int GetCurrentUserId()
    {
        var claim = User.FindFirstValue(ClaimTypes.NameIdentifier);
        return int.TryParse(claim, out var id) ? id : 0;
    }
}

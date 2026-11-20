using System.Security.Claims;
using BookClub.Data;
using BookClub.Models.Entities;
using BookClub.Models.Enums;
using BookClub.Models.ViewModels;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace BookClub.Controllers;

public class AccountController : Controller
{
    private readonly AppDbContext _context;
    private readonly IPasswordHasher<User> _passwordHasher;

    public AccountController(AppDbContext context, IPasswordHasher<User> passwordHasher)
    {
        _context = context;
        _passwordHasher = passwordHasher;
    }

    [HttpGet]
    public IActionResult Register()
    {
        if (User.Identity?.IsAuthenticated == true)
        {
            return RedirectToAction("Index", "Club");
        }

        return View(new RegisterViewModel());
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Register(RegisterViewModel model)
    {
        if (User.Identity?.IsAuthenticated == true)
        {
            return RedirectToAction("Index", "Club");
        }

        if (!ModelState.IsValid)
        {
            return View(model);
        }

        var normalizedEmail = model.Email.Trim().ToLowerInvariant();
        var emailExists = await _context.Users.AnyAsync(u => u.Email == normalizedEmail);
        if (emailExists)
        {
            ModelState.AddModelError(nameof(model.Email), "Este e-mail já está cadastrado. Tente fazer login.");
            return View(model);
        }

        var user = new User
        {
            Name = model.Name.Trim(),
            Surname = model.Surname.Trim(),
            Email = normalizedEmail
        };
        user.PasswordHash = _passwordHasher.HashPassword(user, model.Password);

        _context.Users.Add(user);
        await _context.SaveChangesAsync();

        await SignInUserAsync(user, isPersistent: true);
        TempData["SuccessMessage"] = $"Bem-vindo(a), {user.Name}! Sua conta foi criada com sucesso.";

        return RedirectToAction("Index", "Club");
    }

    [HttpGet]
    public IActionResult Login(string? returnUrl = null)
    {
        if (User.Identity?.IsAuthenticated == true)
        {
            return RedirectToAction("Index", "Club");
        }

        return View(new LoginViewModel { ReturnUrl = returnUrl });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Login(LoginViewModel model)
    {
        if (User.Identity?.IsAuthenticated == true)
        {
            return RedirectToAction("Index", "Club");
        }

        if (!ModelState.IsValid)
        {
            return View(model);
        }

        var normalizedEmail = model.Email.Trim().ToLowerInvariant();
        var user = await _context.Users.FirstOrDefaultAsync(u => u.Email == normalizedEmail);

        if (user is null)
        {
            ModelState.AddModelError(string.Empty, "E-mail ou senha inválidos.");
            return View(model);
        }

        var verification = _passwordHasher.VerifyHashedPassword(user, user.PasswordHash, model.Password);
        if (verification == PasswordVerificationResult.Failed)
        {
            ModelState.AddModelError(string.Empty, "E-mail ou senha inválidos.");
            return View(model);
        }

        if (verification == PasswordVerificationResult.SuccessRehashNeeded)
        {
            user.PasswordHash = _passwordHasher.HashPassword(user, model.Password);
            await _context.SaveChangesAsync();
        }

        await SignInUserAsync(user, model.RememberMe);
        TempData["SuccessMessage"] = $"Olá, {user.Name}! Bom te ver por aqui.";

        if (!string.IsNullOrWhiteSpace(model.ReturnUrl) && Url.IsLocalUrl(model.ReturnUrl))
        {
            return Redirect(model.ReturnUrl);
        }

        return RedirectToAction("Index", "Club");
    }

    [HttpPost]
    [Authorize]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Logout()
    {
        await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
        TempData["InfoMessage"] = "Você saiu da sua conta com segurança.";
        return RedirectToAction("Index", "Home");
    }

    [HttpGet]
    [Authorize]
    public async Task<IActionResult> Profile()
    {
        var userId = GetCurrentUserId();
        var user = await _context.Users
            .AsNoTracking()
            .FirstOrDefaultAsync(u => u.Id == userId);

        if (user is null)
        {
            await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            return RedirectToAction(nameof(Login));
        }

        var clubs = await _context.UserClubs
            .AsNoTracking()
            .Where(uc => uc.UserId == userId && uc.Status == MembershipStatus.Active)
            .OrderByDescending(uc => uc.Role == ClubRole.Owner)
            .ThenBy(uc => uc.Club.Name)
            .Select(uc => new ClubSummaryItemViewModel
            {
                Id = uc.ClubId,
                Name = uc.Club.Name,
                Description = uc.Club.Description,
                IsPrivate = uc.Club.IsPrivate,
                MemberCount = uc.Club.UserClubs.Count(m => m.Status == MembershipStatus.Active),
                CurrentBookTitle = uc.Club.ClubBooks
                    .Where(cb => cb.Status == ClubBookStatus.Current)
                    .OrderBy(cb => cb.DisplayOrder)
                    .Select(cb => cb.Book.Name)
                    .FirstOrDefault(),
                UserRole = uc.Role,
                MembershipStatus = uc.Status
            })
            .ToListAsync();

        var viewModel = new ProfileViewModel
        {
            FullName = user.FullName,
            Email = user.Email,
            CreatedAt = user.CreatedAt,
            Clubs = clubs
        };

        return View(viewModel);
    }

    [HttpGet]
    public IActionResult AccessDenied()
    {
        TempData["ErrorMessage"] = "Você não tem permissão para acessar este recurso.";
        return RedirectToAction("Index", "Club");
    }

    private async Task SignInUserAsync(User user, bool isPersistent)
    {
        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new(ClaimTypes.Name, user.FullName),
            new(ClaimTypes.GivenName, user.Name),
            new(ClaimTypes.Email, user.Email)
        };

        var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
        var principal = new ClaimsPrincipal(identity);

        await HttpContext.SignInAsync(
            CookieAuthenticationDefaults.AuthenticationScheme,
            principal,
            new AuthenticationProperties
            {
                IsPersistent = isPersistent,
                ExpiresUtc = DateTimeOffset.UtcNow.AddDays(isPersistent ? 14 : 1)
            });
    }

    private int GetCurrentUserId()
    {
        var claim = User.FindFirstValue(ClaimTypes.NameIdentifier);
        return int.TryParse(claim, out var id) ? id : 0;
    }
}

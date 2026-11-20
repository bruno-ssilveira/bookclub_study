using System.ComponentModel.DataAnnotations;
using BookClub.Models.Enums;

namespace BookClub.Models.ViewModels;

public class ClubSummaryItemViewModel
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public bool IsPrivate { get; set; }
    public int MemberCount { get; set; }
    public int PendingRequestsCount { get; set; }
    public string? CurrentBookTitle { get; set; }
    public string? CurrentBookCoverUrl { get; set; }
    public ClubRole? UserRole { get; set; }
    public MembershipStatus? MembershipStatus { get; set; }
    public bool IsMember => MembershipStatus == Enums.MembershipStatus.Active;
    public bool IsPending => MembershipStatus == Enums.MembershipStatus.Pending;
    public bool IsBanned => MembershipStatus == Enums.MembershipStatus.Banned;
}

public class MyClubsViewModel
{
    public List<ClubSummaryItemViewModel> Clubs { get; set; } = new();
    public List<ClubSummaryItemViewModel> PendingClubs { get; set; } = new();
    public string? InviteCode { get; set; }
}

public class ExploreClubsViewModel
{
    public string? Search { get; set; }
    public List<ClubSummaryItemViewModel> PublicClubs { get; set; } = new();
}

public class ClubFormViewModel
{
    public int? Id { get; set; }

    [Required(ErrorMessage = "Informe o nome do clube.")]
    [StringLength(120, MinimumLength = 3, ErrorMessage = "O nome do clube deve ter entre 3 e 120 caracteres.")]
    [Display(Name = "Nome do clube")]
    public string Name { get; set; } = string.Empty;

    [StringLength(2000, ErrorMessage = "A descrição pode ter no máximo 2000 caracteres.")]
    [Display(Name = "Descrição e regras do clube")]
    public string? Description { get; set; }

    [Display(Name = "Clube privado (acesso apenas por código de convite)")]
    public bool IsPrivate { get; set; }
}

public class ClubBookItemViewModel
{
    public int ClubBookId { get; set; }
    public int BookId { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Author { get; set; } = string.Empty;
    public string? Synopsis { get; set; }
    public int? Pages { get; set; }
    public string? CoverUrl { get; set; }
    public int? PublishedYear { get; set; }
    public string? Isbn { get; set; }
    public ClubBookStatus Status { get; set; }
    public DateOnly? StartDate { get; set; }
    public DateOnly? TargetFinishDate { get; set; }
    public int DisplayOrder { get; set; }
}

public class ClubMemberItemViewModel
{
    public int UserId { get; set; }
    public string FullName { get; set; } = string.Empty;
    public string Initials { get; set; } = string.Empty;
    public ClubRole Role { get; set; }
    public MembershipStatus Status { get; set; }
    public DateTime JoinedAt { get; set; }
}

public class ClubDetailsViewModel
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public bool IsPrivate { get; set; }
    public string InviteCode { get; set; } = string.Empty;
    public ClubRole? CurrentUserRole { get; set; }
    public MembershipStatus? CurrentUserStatus { get; set; }
    public bool IsOwner => CurrentUserStatus == MembershipStatus.Active && CurrentUserRole == ClubRole.Owner;
    public bool IsMember => CurrentUserStatus == MembershipStatus.Active;
    public bool IsPending => CurrentUserStatus == MembershipStatus.Pending;
    public string ActiveTab { get; set; } = "current";

    public List<ClubBookItemViewModel> CurrentBooks { get; set; } = new();
    public List<ClubBookItemViewModel> UpcomingBooks { get; set; } = new();
    public List<ClubBookItemViewModel> FinishedBooks { get; set; } = new();
    public List<ClubMemberItemViewModel> Members { get; set; } = new();
    public List<ClubMemberItemViewModel> PendingRequests { get; set; } = new();
    public List<ClubMemberItemViewModel> BannedMembers { get; set; } = new();
}

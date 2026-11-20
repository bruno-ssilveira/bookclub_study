using BookClub.Models.Enums;

namespace BookClub.Models.Entities;

public class UserClub : BaseEntity
{
    public int UserId { get; set; }
    public User User { get; set; } = null!;

    public int ClubId { get; set; }
    public Club Club { get; set; } = null!;

    public ClubRole Role { get; set; } = ClubRole.Member;
    public MembershipStatus Status { get; set; } = MembershipStatus.Active;
}

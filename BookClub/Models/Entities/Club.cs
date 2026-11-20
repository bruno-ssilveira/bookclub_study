namespace BookClub.Models.Entities;

public class Club : BaseEntity
{
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public bool IsPrivate { get; set; }
    public string InviteCode { get; set; } = string.Empty;

    public ICollection<UserClub> UserClubs { get; set; } = new List<UserClub>();
    public ICollection<ClubBook> ClubBooks { get; set; } = new List<ClubBook>();
}

using BookClub.Models.Enums;

namespace BookClub.Models.Entities;

public class ClubBook : BaseEntity
{
    public int ClubId { get; set; }
    public Club Club { get; set; } = null!;

    public int BookId { get; set; }
    public Book Book { get; set; } = null!;

    public ClubBookStatus Status { get; set; } = ClubBookStatus.Upcoming;
    public DateOnly? StartDate { get; set; }
    public DateOnly? TargetFinishDate { get; set; }
    public int DisplayOrder { get; set; }
}

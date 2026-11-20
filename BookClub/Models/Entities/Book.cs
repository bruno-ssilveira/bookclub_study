namespace BookClub.Models.Entities;

public class Book : BaseEntity
{
    public string? ExternalId { get; set; }
    public string? Isbn { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Author { get; set; } = string.Empty;
    public string? Synopsis { get; set; }
    public int? Pages { get; set; }
    public string? CoverUrl { get; set; }
    public int? PublishedYear { get; set; }

    public ICollection<ClubBook> ClubBooks { get; set; } = new List<ClubBook>();
}

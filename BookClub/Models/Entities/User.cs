namespace BookClub.Models.Entities;

public class User : BaseEntity
{
    public string Name { get; set; } = string.Empty;
    public string Surname { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string PasswordHash { get; set; } = string.Empty;

    public string FullName => $"{Name} {Surname}".Trim();

    public ICollection<UserClub> UserClubs { get; set; } = new List<UserClub>();
}

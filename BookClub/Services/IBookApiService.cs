namespace BookClub.Services;

public class BookSearchResultDto
{
    public string? ExternalId { get; set; }
    public string? Isbn { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Author { get; set; } = string.Empty;
    public string? Synopsis { get; set; }
    public int? Pages { get; set; }
    public string? CoverUrl { get; set; }
    public int? PublishedYear { get; set; }
    public string Source { get; set; } = "Google Books";
}

public interface IBookApiService
{
    Task<(IReadOnlyList<BookSearchResultDto> Results, string? WarningMessage)> SearchBooksAsync(
        string query,
        CancellationToken cancellationToken = default);
}

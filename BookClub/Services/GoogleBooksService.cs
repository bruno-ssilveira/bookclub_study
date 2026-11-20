using System.Text.Json;
using System.Text.RegularExpressions;

namespace BookClub.Services;

public partial class GoogleBooksService : IBookApiService
{
    private readonly HttpClient _httpClient;
    private readonly IConfiguration _configuration;
    private readonly ILogger<GoogleBooksService> _logger;

    public GoogleBooksService(
        HttpClient httpClient,
        IConfiguration configuration,
        ILogger<GoogleBooksService> logger)
    {
        _httpClient = httpClient;
        _configuration = configuration;
        _logger = logger;
    }

    public async Task<(IReadOnlyList<BookSearchResultDto> Results, string? WarningMessage)> SearchBooksAsync(
        string query,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(query))
        {
            return (Array.Empty<BookSearchResultDto>(), null);
        }

        var trimmedQuery = query.Trim();

        try
        {
            var googleResults = await SearchGoogleBooksAsync(trimmedQuery, cancellationToken);
            if (googleResults.Count > 0)
            {
                return (googleResults, null);
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Falha ao consultar a Google Books API para a busca '{Query}'. Tentando fallback Open Library.", trimmedQuery);
        }

        try
        {
            var openLibraryResults = await SearchOpenLibraryAsync(trimmedQuery, cancellationToken);
            if (openLibraryResults.Count > 0)
            {
                return (openLibraryResults, null);
            }

            return (Array.Empty<BookSearchResultDto>(),
                "Nenhum livro encontrado na busca automática. Você pode preencher os dados manualmente abaixo.");
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Falha também no fallback Open Library para a busca '{Query}'.", trimmedQuery);
            return (Array.Empty<BookSearchResultDto>(),
                "O serviço externo de busca de livros está temporariamente indisponível. Utilize o cadastro manual abaixo.");
        }
    }

    private async Task<List<BookSearchResultDto>> SearchGoogleBooksAsync(string query, CancellationToken cancellationToken)
    {
        var apiKey = _configuration["GoogleBooks:ApiKey"];
        var encodedQuery = Uri.EscapeDataString(query);
        var url = $"https://www.googleapis.com/books/v1/volumes?q={encodedQuery}&maxResults=10&printType=books";

        if (!string.IsNullOrWhiteSpace(apiKey))
        {
            url += $"&key={Uri.EscapeDataString(apiKey)}";
        }

        using var response = await _httpClient.GetAsync(url, cancellationToken);
        response.EnsureSuccessStatusCode();

        await using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
        using var doc = await JsonDocument.ParseAsync(stream, cancellationToken: cancellationToken);

        var results = new List<BookSearchResultDto>();
        if (!doc.RootElement.TryGetProperty("items", out var items) || items.ValueKind != JsonValueKind.Array)
        {
            return results;
        }

        foreach (var item in items.EnumerateArray())
        {
            if (!item.TryGetProperty("volumeInfo", out var info))
            {
                continue;
            }

            var title = GetString(info, "title");
            if (string.IsNullOrWhiteSpace(title))
            {
                continue;
            }

            var subtitle = GetString(info, "subtitle");
            var fullTitle = string.IsNullOrWhiteSpace(subtitle) ? title : $"{title}: {subtitle}";

            var authors = new List<string>();
            if (info.TryGetProperty("authors", out var authorsEl) && authorsEl.ValueKind == JsonValueKind.Array)
            {
                foreach (var author in authorsEl.EnumerateArray())
                {
                    var name = author.GetString();
                    if (!string.IsNullOrWhiteSpace(name))
                    {
                        authors.Add(name);
                    }
                }
            }

            string? isbn13 = null;
            string? isbn10 = null;
            if (info.TryGetProperty("industryIdentifiers", out var identifiers) && identifiers.ValueKind == JsonValueKind.Array)
            {
                foreach (var id in identifiers.EnumerateArray())
                {
                    var type = GetString(id, "type");
                    var val = GetString(id, "identifier");
                    if (type == "ISBN_13" && !string.IsNullOrWhiteSpace(val)) isbn13 = val;
                    if (type == "ISBN_10" && !string.IsNullOrWhiteSpace(val)) isbn10 = val;
                }
            }

            string? coverUrl = null;
            if (info.TryGetProperty("imageLinks", out var imageLinks) && imageLinks.ValueKind == JsonValueKind.Object)
            {
                coverUrl = GetString(imageLinks, "thumbnail") ?? GetString(imageLinks, "smallThumbnail");
                if (!string.IsNullOrWhiteSpace(coverUrl) && coverUrl.StartsWith("http://", StringComparison.OrdinalIgnoreCase))
                {
                    coverUrl = "https://" + coverUrl[7..];
                }
            }

            int? pages = null;
            if (info.TryGetProperty("pageCount", out var pageEl) && pageEl.ValueKind == JsonValueKind.Number && pageEl.TryGetInt32(out var p) && p > 0)
            {
                pages = p;
            }

            int? publishedYear = ParseYear(GetString(info, "publishedDate"));

            var synopsis = GetString(info, "description");
            if (!string.IsNullOrWhiteSpace(synopsis))
            {
                synopsis = HtmlTagRegex().Replace(synopsis, string.Empty).Trim();
            }

            results.Add(new BookSearchResultDto
            {
                ExternalId = Truncate(GetString(item, "id"), 100),
                Isbn = Truncate(isbn13 ?? isbn10, 20),
                Title = Truncate(fullTitle, 200) ?? title,
                Author = Truncate(authors.Count > 0 ? string.Join(", ", authors) : "Autor desconhecido", 200)!,
                Synopsis = synopsis,
                Pages = pages,
                CoverUrl = Truncate(coverUrl, 500),
                PublishedYear = publishedYear,
                Source = "Google Books"
            });
        }

        return results;
    }

    private async Task<List<BookSearchResultDto>> SearchOpenLibraryAsync(string query, CancellationToken cancellationToken)
    {
        var encodedQuery = Uri.EscapeDataString(query);
        var url = $"https://openlibrary.org/search.json?q={encodedQuery}&limit=10&fields=key,title,author_name,first_publish_year,isbn,cover_i,number_of_pages_median";

        using var response = await _httpClient.GetAsync(url, cancellationToken);
        response.EnsureSuccessStatusCode();

        await using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
        using var doc = await JsonDocument.ParseAsync(stream, cancellationToken: cancellationToken);

        var results = new List<BookSearchResultDto>();
        if (!doc.RootElement.TryGetProperty("docs", out var docs) || docs.ValueKind != JsonValueKind.Array)
        {
            return results;
        }

        foreach (var item in docs.EnumerateArray())
        {
            var title = GetString(item, "title");
            if (string.IsNullOrWhiteSpace(title))
            {
                continue;
            }

            var authors = new List<string>();
            if (item.TryGetProperty("author_name", out var authorsEl) && authorsEl.ValueKind == JsonValueKind.Array)
            {
                foreach (var author in authorsEl.EnumerateArray())
                {
                    var name = author.GetString();
                    if (!string.IsNullOrWhiteSpace(name))
                    {
                        authors.Add(name);
                    }
                }
            }

            string? isbn = null;
            if (item.TryGetProperty("isbn", out var isbnEl) && isbnEl.ValueKind == JsonValueKind.Array)
            {
                foreach (var val in isbnEl.EnumerateArray())
                {
                    var s = val.GetString();
                    if (!string.IsNullOrWhiteSpace(s) && s.Length <= 20)
                    {
                        isbn = s;
                        if (s.Length == 13) break;
                    }
                }
            }

            string? coverUrl = null;
            if (item.TryGetProperty("cover_i", out var coverEl) && coverEl.ValueKind == JsonValueKind.Number && coverEl.TryGetInt64(out var coverId))
            {
                coverUrl = $"https://covers.openlibrary.org/b/id/{coverId}-M.jpg";
            }

            int? pages = null;
            if (item.TryGetProperty("number_of_pages_median", out var pagesEl) && pagesEl.ValueKind == JsonValueKind.Number && pagesEl.TryGetInt32(out var p) && p > 0)
            {
                pages = p;
            }

            int? publishedYear = null;
            if (item.TryGetProperty("first_publish_year", out var yearEl) && yearEl.ValueKind == JsonValueKind.Number && yearEl.TryGetInt32(out var y))
            {
                publishedYear = y;
            }

            var key = GetString(item, "key");
            var externalId = !string.IsNullOrWhiteSpace(key) ? $"ol:{key.TrimStart('/')}" : null;

            results.Add(new BookSearchResultDto
            {
                ExternalId = Truncate(externalId, 100),
                Isbn = Truncate(isbn, 20),
                Title = Truncate(title, 200) ?? title,
                Author = Truncate(authors.Count > 0 ? string.Join(", ", authors) : "Autor desconhecido", 200)!,
                Synopsis = null,
                Pages = pages,
                CoverUrl = Truncate(coverUrl, 500),
                PublishedYear = publishedYear,
                Source = "Open Library"
            });
        }

        return results;
    }

    private static string? GetString(JsonElement element, string propertyName)
    {
        return element.TryGetProperty(propertyName, out var prop) && prop.ValueKind == JsonValueKind.String
            ? prop.GetString()
            : null;
    }

    private static int? ParseYear(string? rawDate)
    {
        if (string.IsNullOrWhiteSpace(rawDate) || rawDate.Length < 4)
        {
            return null;
        }

        return int.TryParse(rawDate[..4], out var year) && year is >= 1000 and <= 2100 ? year : null;
    }

    private static string? Truncate(string? value, int maxLength)
    {
        if (string.IsNullOrWhiteSpace(value)) return null;
        var trimmed = value.Trim();
        return trimmed.Length <= maxLength ? trimmed : trimmed[..maxLength];
    }

    [GeneratedRegex("<.*?>", RegexOptions.Compiled)]
    private static partial Regex HtmlTagRegex();
}

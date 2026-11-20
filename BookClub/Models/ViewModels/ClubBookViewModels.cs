using System.ComponentModel.DataAnnotations;
using BookClub.Models.Enums;

namespace BookClub.Models.ViewModels;

public class AddClubBookViewModel
{
    public int ClubId { get; set; }
    public string ClubName { get; set; } = string.Empty;

    public string? ExternalId { get; set; }

    [StringLength(20, ErrorMessage = "O ISBN deve ter no máximo 20 caracteres.")]
    [Display(Name = "ISBN (opcional)")]
    public string? Isbn { get; set; }

    [Required(ErrorMessage = "Informe o título do livro.")]
    [StringLength(200, ErrorMessage = "O título deve ter no máximo 200 caracteres.")]
    [Display(Name = "Título do livro")]
    public string Name { get; set; } = string.Empty;

    [Required(ErrorMessage = "Informe o autor do livro.")]
    [StringLength(200, ErrorMessage = "O nome do autor deve ter no máximo 200 caracteres.")]
    [Display(Name = "Autor(a)")]
    public string Author { get; set; } = string.Empty;

    [Display(Name = "Sinopse (opcional)")]
    public string? Synopsis { get; set; }

    [Range(1, 20000, ErrorMessage = "Informe uma quantidade válida de páginas.")]
    [Display(Name = "Número de páginas (opcional)")]
    public int? Pages { get; set; }

    [StringLength(500, ErrorMessage = "A URL da capa deve ter no máximo 500 caracteres.")]
    [Display(Name = "URL da capa (preenchido automaticamente pela busca)")]
    public string? CoverUrl { get; set; }

    [Range(1000, 2100, ErrorMessage = "Informe um ano de publicação válido.")]
    [Display(Name = "Ano de publicação (opcional)")]
    public int? PublishedYear { get; set; }

    [Required(ErrorMessage = "Selecione o status no cronograma.")]
    [Display(Name = "Status no cronograma")]
    public ClubBookStatus Status { get; set; } = ClubBookStatus.Upcoming;

    [DataType(DataType.Date)]
    [Display(Name = "Data de início (opcional)")]
    public DateOnly? StartDate { get; set; }

    [DataType(DataType.Date)]
    [Display(Name = "Término / encontro (opcional)")]
    public DateOnly? TargetFinishDate { get; set; }

    [Range(0, 9999, ErrorMessage = "Informe uma ordem entre 0 e 9999.")]
    [Display(Name = "Ordem na fila de próximas leituras")]
    public int DisplayOrder { get; set; }
}

public class EditClubBookViewModel
{
    public int Id { get; set; }
    public int ClubId { get; set; }
    public string ClubName { get; set; } = string.Empty;

    public string BookTitle { get; set; } = string.Empty;
    public string BookAuthor { get; set; } = string.Empty;
    public string? BookCoverUrl { get; set; }

    [Required(ErrorMessage = "Selecione o status no cronograma.")]
    [Display(Name = "Status no cronograma")]
    public ClubBookStatus Status { get; set; }

    [DataType(DataType.Date)]
    [Display(Name = "Data de início (opcional)")]
    public DateOnly? StartDate { get; set; }

    [DataType(DataType.Date)]
    [Display(Name = "Término / encontro (opcional)")]
    public DateOnly? TargetFinishDate { get; set; }

    [Range(0, 9999, ErrorMessage = "Informe uma ordem entre 0 e 9999.")]
    [Display(Name = "Ordem na fila de próximas leituras")]
    public int DisplayOrder { get; set; }
}

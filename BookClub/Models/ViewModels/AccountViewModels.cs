using System.ComponentModel.DataAnnotations;

namespace BookClub.Models.ViewModels;

public class RegisterViewModel
{
    [Required(ErrorMessage = "Informe seu nome.")]
    [StringLength(100, MinimumLength = 2, ErrorMessage = "O nome deve ter entre 2 e 100 caracteres.")]
    [Display(Name = "Nome")]
    public string Name { get; set; } = string.Empty;

    [Required(ErrorMessage = "Informe seu sobrenome.")]
    [StringLength(100, MinimumLength = 2, ErrorMessage = "O sobrenome deve ter entre 2 e 100 caracteres.")]
    [Display(Name = "Sobrenome")]
    public string Surname { get; set; } = string.Empty;

    [Required(ErrorMessage = "Informe seu e-mail.")]
    [EmailAddress(ErrorMessage = "Informe um endereço de e-mail válido.")]
    [StringLength(150, ErrorMessage = "O e-mail deve ter no máximo 150 caracteres.")]
    [Display(Name = "E-mail")]
    public string Email { get; set; } = string.Empty;

    [Required(ErrorMessage = "Crie uma senha.")]
    [StringLength(100, MinimumLength = 6, ErrorMessage = "A senha deve ter pelo menos 6 caracteres.")]
    [DataType(DataType.Password)]
    [Display(Name = "Senha")]
    public string Password { get; set; } = string.Empty;

    [Required(ErrorMessage = "Confirme sua senha.")]
    [Compare(nameof(Password), ErrorMessage = "As senhas não conferem.")]
    [DataType(DataType.Password)]
    [Display(Name = "Confirmar senha")]
    public string ConfirmPassword { get; set; } = string.Empty;
}

public class LoginViewModel
{
    [Required(ErrorMessage = "Informe seu e-mail.")]
    [EmailAddress(ErrorMessage = "Informe um endereço de e-mail válido.")]
    [Display(Name = "E-mail")]
    public string Email { get; set; } = string.Empty;

    [Required(ErrorMessage = "Informe sua senha.")]
    [DataType(DataType.Password)]
    [Display(Name = "Senha")]
    public string Password { get; set; } = string.Empty;

    [Display(Name = "Manter conectado")]
    public bool RememberMe { get; set; } = true;

    public string? ReturnUrl { get; set; }
}

public class ProfileViewModel
{
    public string FullName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public List<ClubSummaryItemViewModel> Clubs { get; set; } = new();
}

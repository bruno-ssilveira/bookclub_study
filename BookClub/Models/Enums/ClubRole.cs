using System.ComponentModel.DataAnnotations;

namespace BookClub.Models.Enums;

public enum ClubRole
{
    [Display(Name = "Administrador")]
    Owner = 1,

    [Display(Name = "Membro")]
    Member = 2
}

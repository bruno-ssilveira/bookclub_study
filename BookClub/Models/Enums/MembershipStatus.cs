using System.ComponentModel.DataAnnotations;

namespace BookClub.Models.Enums;

public enum MembershipStatus
{
    [Display(Name = "Ativo")]
    Active = 1,

    [Display(Name = "Pendente")]
    Pending = 2,

    [Display(Name = "Banido")]
    Banned = 3
}

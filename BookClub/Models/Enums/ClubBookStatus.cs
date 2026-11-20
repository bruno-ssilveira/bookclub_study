using System.ComponentModel.DataAnnotations;

namespace BookClub.Models.Enums;

public enum ClubBookStatus
{
    [Display(Name = "Lendo Agora")]
    Current = 1,

    [Display(Name = "Próximo")]
    Upcoming = 2,

    [Display(Name = "Concluído")]
    Finished = 3
}

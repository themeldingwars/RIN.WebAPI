using System.ComponentModel.DataAnnotations;

namespace RIN.WebAPI.Models.ClientApi;

public class KickArmyMembersReq
{
    [Required] public string[] character_guids { get; set; }

    public long[] CharacterGuids => character_guids.Select(long.Parse).ToArray();
}

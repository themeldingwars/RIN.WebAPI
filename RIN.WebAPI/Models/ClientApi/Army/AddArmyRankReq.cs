namespace RIN.WebAPI.Controllers;

public class AddArmyRankReq
{
    public required string name        { get; set; }
    public required bool   can_edit    { get; set; }
    public required bool   can_invite  { get; set; }
    public required bool   can_kick    { get; set; }
    public required short  position    { get; set; }
    public required bool   is_officer  { get; set; }
    public required bool   can_promote { get; set; }
}

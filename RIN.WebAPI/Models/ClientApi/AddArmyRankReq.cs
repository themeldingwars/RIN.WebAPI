namespace RIN.WebAPI.Controllers;

public class AddArmyRankReq
{
    public bool can_promote { get; set; }
    public bool can_edit { get; set; }
    public bool is_officer { get; set; }
    public string name { get; set; }
    public bool can_invite { get; set; }
    public bool can_kick { get; set; }
    public int position { get; set; }
}
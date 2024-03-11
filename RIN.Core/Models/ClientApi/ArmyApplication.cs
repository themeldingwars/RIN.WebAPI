namespace RIN.Core.Models.ClientApi;

public class ArmyApplication
{
    public long    id        { get; set; }
    public long    army_guid { get; set; }
    public string? army_name { get; set; }
    public string? name      { get; set; }
    public string  message   { get; set; }
    public string  direction { get; set; }
}

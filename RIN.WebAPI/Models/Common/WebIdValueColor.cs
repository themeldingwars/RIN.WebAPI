namespace RIN.WebAPI.Models.Common
{
    public class WebIdValueColor
    {
        public int      id    { get; set; }
        public WebColor value { get; set; }

        public WebIdValueColor()
        {
        }

        public WebIdValueColor(int id, uint color)
        {
            this.id = id;
            value   = new WebColor(color);
        }
    }
}
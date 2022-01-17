namespace RIN.WebAPI.Models.Common
{
    public class WebIdValueColorId
    {
        public int        id    { get; set; }
        public WebColorId color { get; set; }

        public WebIdValueColorId()
        {
        }

        public WebIdValueColorId(int id, int colorId, uint color)
        {
            this.id    = id;
            this.color = new WebColorId(colorId, color);
        }
    }
}
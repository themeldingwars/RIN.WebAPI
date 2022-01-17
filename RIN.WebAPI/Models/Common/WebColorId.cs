namespace RIN.WebAPI.Models.Common
{
    public class WebColorId
    {
        public int  id    { get; set; }
        public uint value { get; set; }

        public WebColorId()
        {
        }

        public WebColorId(int id, uint color)
        {
            this.id    = id;
            this.value = color;
        }
    }
}
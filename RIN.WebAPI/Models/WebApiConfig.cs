namespace RIN.WebAPI.Models
{
    public class WebApiConfig
    {
        public string BaseURL       { get; set; }
        public bool   VerifyRed5Sig { get; set; } = true;

        public string DBConnectionStr { get; set; }

    #region Sub Classes

        public class ServerDefaultSettings
        {
            public int CharaterLimitPerAccount { get; set; } = 2;
            public int CharaterNameMaxLength   { get; set; } = 40;
            public int CharaterNameMinLength   { get; set; } = 1;
        }

    #endregion
    }
}
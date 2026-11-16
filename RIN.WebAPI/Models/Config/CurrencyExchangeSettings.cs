using RIN.Core.DB;

namespace RIN.WebAPI.Models.Config
{
    /// <summary>
    /// The crystite to credits exchange. Only the web API reads it: the limit is reserved when a purchase is opened,
    /// so settling one on the internal API doesn't need it.
    /// The defaults are update 1.6's: a credit cost 25 crystite up to update 1.3 and 250 from 1.6.
    /// </summary>
    public class CurrencyExchangeSettings
    {
        public const string NAME = "CurrencyExchange";

        public int CrystitePerCredit { get; set; } = 250;

        // Credits per character per UTC day. The limit is what keeps credits scarce, crystite piles up for everyone.
        public int DailyCreditLimit { get; set; } = 1000;

        /// <summary>
        /// The rates the exchange offers. It is one way, crystite into credits: the 1962 client only ever asks for
        /// that, and its confirm prompt only adds up when ToQuantity is 1.
        /// </summary>
        public IReadOnlyList<CurrencyExchangeRate> Rates =>
        [
            new(CurrencyExchangeRates.CRYSTITE_SDB_ID, CurrencyExchangeRates.CREDITS_SDB_ID,
                FromQuantity: CrystitePerCredit, ToQuantity: 1, DailyLimit: DailyCreditLimit)
        ];

        public CurrencyExchangeRate? FindRate(int fromSdbId, int toSdbId)
            => Rates.FirstOrDefault(r => r.FromSdbId == fromSdbId && r.ToSdbId == toSdbId);

        public static void Register(IServiceCollection services, IConfiguration configuration)
        {
            services.AddOptions<CurrencyExchangeSettings>()
                    .Bind(configuration.GetSection(NAME))
                    .Validate(s => s.CrystitePerCredit > 0, $"{NAME}:{nameof(CrystitePerCredit)} has to be at least 1.")
                    .Validate(s => s.DailyCreditLimit >= 0, $"{NAME}:{nameof(DailyCreditLimit)} can't be negative.")
                    // A purchase's crystite is an int all the way to the game server's wallet
                    .Validate(s => (long)s.CrystitePerCredit * s.DailyCreditLimit <= int.MaxValue,
                              $"{NAME}: a day's credits can't cost more than {int.MaxValue} crystite.")
                    .ValidateOnStart();
        }
    }
}

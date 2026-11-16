using Microsoft.AspNetCore.Mvc;
using RIN.Core;
using RIN.Core.DB;
using RIN.WebAPI.Models.ClientApi;
using RIN.WebAPI.Utils;

namespace RIN.WebAPI.Controllers
{
    public partial class ClientAPiV3
    {
        [HttpGet("characters/{characterGuid}/currency_exchange")]
        [R5SigAuthRequired]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        public async Task<object> GetCurrencyExchange(long characterGuid)
        {
            if (characterGuid != GetCid())
            {
                return ReturnError(Error.Codes.ERR_UNKNOWN, "You can only view your own currency exchange.", StatusCodes.Status403Forbidden);
            }

            var rates = new List<CurrencyExchangeRateResp>();
            foreach (var rate in CurrencyExchange.Rates)
            {
                var bought = await Db.GetCurrencyBoughtToday(characterGuid, rate);
                if (bought == null)
                {
                    return ReturnError(Error.Codes.ERR_UNKNOWN, "The currency exchange is unavailable.");
                }

                rates.Add(new CurrencyExchangeRateResp
                {
                    from_sdb_id   = rate.FromSdbId,
                    to_sdb_id     = rate.ToSdbId,
                    from_quantity = rate.FromQuantity,
                    to_quantity   = rate.ToQuantity,
                    daily_limit   = rate.DailyLimit / rate.ToQuantity,
                    remaining     = rate.RemainingExchanges(bought.Value),
                    active        = true
                });
            }

            return rates;
        }

        [HttpPost("characters/{characterGuid}/currency_exchange")]
        [R5SigAuthRequired]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status422UnprocessableEntity)]
        public async Task<object> PostCurrencyExchange(long characterGuid, [FromBody] CurrencyExchangeReq req)
        {
            if (characterGuid != GetCid())
            {
                return ReturnError(Error.Codes.ERR_UNKNOWN, "You can only exchange your own currency.", StatusCodes.Status403Forbidden);
            }

            var rate = CurrencyExchange.FindRate(req.from_sdb_id, req.to_sdb_id);
            if (rate == null)
            {
                return ReturnError(Error.Codes.ERR_UNKNOWN, "The exchange does not trade those currencies.", StatusCodes.Status400BadRequest);
            }

            // Whole exchanges only, so nothing is paid for that isn't bought
            if (req.quantity <= 0 || req.quantity > int.MaxValue || req.quantity % rate.FromQuantity != 0)
            {
                return ReturnError(Error.Codes.ERR_UNKNOWN, $"The amount has to be a multiple of {rate.FromQuantity}.", StatusCodes.Status400BadRequest);
            }

            var exchanges = (int)(req.quantity / rate.FromQuantity);
            var (status, exchangeId) = await Db.OpenCurrencyExchange(characterGuid, rate, exchanges);

            if (status == CurrencyExchangeStatus.Pending)
            {
                var settled = await WalletChargeWait.UntilSettled(async () =>
                    (status = await Db.GetCurrencyExchangeStatus(exchangeId)) != CurrencyExchangeStatus.Pending);

                if (!settled)
                    status = await Db.AbandonCurrencyExchange(exchangeId, characterGuid);
            }

            Logger.LogInformation("Character {characterGuid} asked to exchange {paid} of {from} for {got} of {to}: {status}",
                characterGuid, exchanges * rate.FromQuantity, rate.FromSdbId, exchanges * rate.ToQuantity, rate.ToSdbId, status);

            switch (status)
            {
                case CurrencyExchangeStatus.Exchanged:
                    var bought = await Db.GetCurrencyBoughtToday(characterGuid, rate) ?? rate.DailyLimit;

                    return new CurrencyExchangeResp
                    {
                        item_sdb_id = rate.ToSdbId,
                        quantity    = exchanges * rate.ToQuantity,
                        remaining   = rate.RemainingExchanges(bought)
                    };

                case CurrencyExchangeStatus.LimitReached:
                    return ReturnError(Error.Codes.ERR_UNKNOWN, "That would go over today's exchange limit.", StatusCodes.Status422UnprocessableEntity);

                case CurrencyExchangeStatus.NotEnough:
                    return ReturnError(Error.Codes.ERR_UNKNOWN, "You don't have enough to pay for that.", StatusCodes.Status422UnprocessableEntity);

                case CurrencyExchangeStatus.NoAnswer:
                    return ReturnError(Error.Codes.ERR_UNKNOWN, "Your game server didn't answer, try again once you are in a zone.", StatusCodes.Status503ServiceUnavailable);

                default:
                    return ReturnError(Error.Codes.ERR_UNKNOWN, "The currency exchange is unavailable.");
            }
        }
    }
}

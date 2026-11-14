using Microsoft.AspNetCore.Mvc;
using RIN.Core;
using RIN.Core.DB;
using RIN.WebAPI.Models.ClientApi;
using RIN.WebAPI.Utils;
using static RIN.Core.DB.RedBeanExchangeRules;

namespace RIN.WebAPI.Controllers
{
    // The client polls the book and the character's orders every 30 seconds while the panel is open. Error codes are
    // client text keys, shown under the quantity box.
    public partial class ClientApiV2
    {
        private const string ERR_NOT_ENOUGH_RED_BEANS = "CURRENCY_EXCHANGE_BAD_REDBEANS";  // Not enough Red Beans
        private const string ERR_NOT_ENOUGH_CREDITS   = "CURRENCY_EXCHANGE_BAD_CREDITS";   // Not enough Credits
        private const string ERR_MIN_PRICE            = "CURRENCY_EXCHANGE_MIN_PPU";       // Min Price/Unit is
        private const string ERR_MAX_PRICE            = "CURRENCY_EXCHANGE_MAX_PPU";       // Max Price/Unit is
        private const string ERR_TOO_MANY_ORDERS      = "ERR_EXCHANGE_RATE_LIMIT_EXCEEDED"; // Too many recent exchanges. Please wait and try again.
        private const string ERR_SERVICE_UNAVAILABLE  = "ERR_UNAVAILABLE";                 // Web Services Unavailable

        [HttpGet("rb_exchanges/rates")]
        [R5SigAuthRequired]
        public async Task<object> GetRedBeanExchangeRates()
        {
            var book = await Db.GetRedBeanExchangeBook();
            if (book == null)
            {
                return ReturnError(ERR_SERVICE_UNAVAILABLE, "The red bean exchange is unavailable.");
            }

            var resp = new RedBeanBookResp();
            foreach (var level in book)
            {
                var side = level.operation == BUY ? resp.buy : resp.sell;
                side.Add(new RedBeanBookLevel { rb = level.rb, price_per_unit = level.price_per_unit });
            }

            return resp;
        }

        [HttpGet("characters/{characterGuid}/rb_exchanges")]
        [R5SigAuthRequired]
        public Task<object> GetRedBeanExchangeOrders(long characterGuid) => GetRedBeanOrders(characterGuid, open: true);

        [HttpGet("characters/{characterGuid}/rb_exchanges/recent")]
        [R5SigAuthRequired]
        public Task<object> GetRedBeanExchangeHistory(long characterGuid) => GetRedBeanOrders(characterGuid, open: false);

        [HttpPost("characters/{characterGuid}/rb_exchanges")]
        [R5SigAuthRequired]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status422UnprocessableEntity)]
        [ProducesResponseType(StatusCodes.Status503ServiceUnavailable)]
        public async Task<object> PostRedBeanExchangeOrder(long characterGuid, [FromBody] RedBeanOrderReq req)
        {
            if (characterGuid != GetCid())
            {
                return ReturnError(Error.Codes.ERR_UNKNOWN, "You can only place your own orders.", StatusCodes.Status403Forbidden);
            }

            if (req.operation != BUY && req.operation != SELL)
            {
                return ReturnError(Error.Codes.ERR_UNKNOWN, "An order has to buy or sell.", StatusCodes.Status400BadRequest);
            }

            if (req.rb < 1 || req.rb > MAX_RB_PER_ORDER || req.rb % 1 != 0)
            {
                return ReturnError(Error.Codes.ERR_UNKNOWN, $"An order is for 1 to {MAX_RB_PER_ORDER} red beans.", StatusCodes.Status400BadRequest);
            }

            if (req.price_per_unit % 1 != 0)
            {
                return ReturnError(Error.Codes.ERR_UNKNOWN, "The price has to be whole credits.", StatusCodes.Status400BadRequest);
            }

            if (req.price_per_unit < MIN_PRICE_PER_UNIT)
            {
                return ReturnError(ERR_MIN_PRICE, $"A red bean sells for at least {MIN_PRICE_PER_UNIT} credits.", StatusCodes.Status422UnprocessableEntity);
            }

            if (req.price_per_unit > MAX_PRICE_PER_UNIT)
            {
                return ReturnError(ERR_MAX_PRICE, $"A red bean sells for at most {MAX_PRICE_PER_UNIT} credits.", StatusCodes.Status422UnprocessableEntity);
            }

            var rb           = (int)req.rb;
            var pricePerUnit = (int)req.price_per_unit;

            RedBeanOrderOutcome outcome;
            long orderId;

            if (req.operation == SELL)
            {
                var (placed, order) = await Db.PlaceRedBeanSellOrder(characterGuid, rb, pricePerUnit);
                (outcome, orderId) = (placed, order?.id ?? 0);
            }
            else
            {
                (outcome, orderId) = await Db.OpenRedBeanBuyOrder(characterGuid, rb, pricePerUnit);

                if (outcome == RedBeanOrderOutcome.Pending)
                {
                    outcome = await WaitForRedBeanBuyOrder(orderId);
                }
            }

            Logger.LogInformation("Character {characterGuid} placed an order to {operation} {rb} red beans at {price}: {outcome} (order {orderId})",
                characterGuid, req.operation, rb, pricePerUnit, outcome, orderId);

            return outcome switch
            {
                RedBeanOrderOutcome.Placed        => new { id = orderId, operation = req.operation, rb, price_per_unit = pricePerUnit },
                RedBeanOrderOutcome.NotEnough     => ReturnError(req.operation == SELL ? ERR_NOT_ENOUGH_RED_BEANS : ERR_NOT_ENOUGH_CREDITS,
                                                                 "You can't pay for that order.", StatusCodes.Status422UnprocessableEntity),
                RedBeanOrderOutcome.TooManyOrders => ReturnError(ERR_TOO_MANY_ORDERS, $"You can have at most {MAX_OPEN_ORDERS} open orders.",
                                                                 StatusCodes.Status422UnprocessableEntity),
                RedBeanOrderOutcome.NoAnswer      => ReturnError(ERR_SERVICE_UNAVAILABLE, "Your game server didn't answer, try again once you are in a zone.",
                                                                 StatusCodes.Status503ServiceUnavailable),
                _                                 => ReturnError(ERR_SERVICE_UNAVAILABLE, "The red bean exchange is unavailable.")
            };
        }

        [HttpPost("characters/{characterGuid}/rb_exchanges/{orderId}/cancel")]
        [R5SigAuthRequired]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status409Conflict)]
        public async Task<object> CancelRedBeanExchangeOrder(long characterGuid, long orderId)
        {
            if (characterGuid != GetCid())
            {
                return ReturnError(Error.Codes.ERR_UNKNOWN, "You can only cancel your own orders.", StatusCodes.Status403Forbidden);
            }

            var (outcome, order) = await Db.CancelRedBeanExchangeOrder(characterGuid, orderId);

            Logger.LogInformation("Character {characterGuid} cancelled red bean order {orderId}: {outcome}", characterGuid, orderId, outcome);

            return outcome switch
            {
                RedBeanOrderOutcome.Cancelled => RedBeanOrderResp.From(order!),
                RedBeanOrderOutcome.NotFound  => ReturnError(Error.Codes.ERR_UNKNOWN, "There is no such order.", StatusCodes.Status404NotFound),
                RedBeanOrderOutcome.NotActive => ReturnError(Error.Codes.ERR_UNKNOWN, "That order can't be cancelled now.", StatusCodes.Status409Conflict),
                _                             => ReturnError(ERR_SERVICE_UNAVAILABLE, "The red bean exchange is unavailable.")
            };
        }

        private async Task<object> GetRedBeanOrders(long characterGuid, bool open)
        {
            if (characterGuid != GetCid())
            {
                return ReturnError(Error.Codes.ERR_UNKNOWN, "You can only view your own orders.", StatusCodes.Status403Forbidden);
            }

            var orders = await Db.GetRedBeanExchangeOrders(characterGuid, open);
            if (orders == null)
            {
                return ReturnError(ERR_SERVICE_UNAVAILABLE, "The red bean exchange is unavailable.");
            }

            return orders.Select(RedBeanOrderResp.From).ToList();
        }

        private async Task<RedBeanOrderOutcome> WaitForRedBeanBuyOrder(long orderId)
        {
            short? status = null;
            var settled = await WalletChargeWait.UntilSettled(async () =>
                (status = await Db.GetRedBeanOrderStatus(orderId)) is not null and not RedBeanOrderStatus.PENDING);

            if (!settled)
            {
                var abandon = await Db.AbandonRedBeanBuyOrder(orderId);
                if (abandon == null)
                    return RedBeanOrderOutcome.Error;

                if (abandon.Value.abandoned)
                    return RedBeanOrderOutcome.NoAnswer;

                status = abandon.Value.status;
            }

            // Only the game server fails an order before it is given up on, when the character can't pay for it
            return status == RedBeanOrderStatus.FAILED ? RedBeanOrderOutcome.NotEnough : RedBeanOrderOutcome.Placed;
        }
    }
}

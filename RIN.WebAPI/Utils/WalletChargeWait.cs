namespace RIN.WebAPI.Utils
{
    // The game server answers within a few hundred milliseconds when the character is online. A charge nobody settles in
    // time is given up on by the caller, and a payment settled after that is delivered back.
    public static class WalletChargeWait
    {
        private static readonly TimeSpan Timeout      = TimeSpan.FromSeconds(5);
        private static readonly TimeSpan PollInterval = TimeSpan.FromMilliseconds(100);

        /// <returns>False when the caller has to give up on the charge</returns>
        public static async Task<bool> UntilSettled(Func<Task<bool>> isSettled)
        {
            var deadline = DateTime.UtcNow + Timeout;
            while (DateTime.UtcNow < deadline)
            {
                await Task.Delay(PollInterval);

                if (await isSettled())
                    return true;
            }

            return false;
        }
    }
}

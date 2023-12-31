using System;

namespace RIN.Core
{
    public class TmwException : Exception
    {
        public Error Error;
        
        public TmwException(string? errorCode = null, string? errorMessage = null)
        {
            Error = new()
            {
                code    = errorCode ?? Error.Codes.ERR_UNKNOWN,
                message = errorMessage ?? Error.Codes.ERR_UNKNOWN
            };
        }
    }
}
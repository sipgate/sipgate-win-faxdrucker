using System;

namespace SipgateFaxdrucker.Utils.Auth
{
    public class InvalidRefreshTokenException : Exception
    {
        public InvalidRefreshTokenException(string message)
            : base(message)
        {
        }
    }
}
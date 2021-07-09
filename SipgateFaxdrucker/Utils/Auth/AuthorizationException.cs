using System;

namespace SipgateFaxdrucker.Utils.Auth
{
    public class AuthorizationException : Exception
    {
        public AuthorizationException(string message)
            : base(message)
        {
        }
    }
}
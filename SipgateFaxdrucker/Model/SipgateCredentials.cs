namespace SipgateFaxdrucker
{
    public class SipgateCredentials
    {
        public string AccessToken { get; }
        public string RefreshToken { get; }

        public SipgateCredentials(string accessToken, string refreshToken)
        {
            AccessToken = accessToken;
            RefreshToken = refreshToken;
        }
    }
}
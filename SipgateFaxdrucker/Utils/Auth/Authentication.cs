using Newtonsoft.Json;
using SipgateFaxdrucker.Properties;
using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Reflection;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace SipgateFaxdrucker.Utils.Auth
{
    public partial class Authentication
    {
        private readonly string _clientId = Settings.Default.ClientId;
#if DEBUG
      //  private static string _keycloakBaseUrl = "https://login.dev.sipgate.com";
#else
       
#endif
      private static string _keycloakBaseUrl = Settings.Default.LoginBaseUrl;
      private readonly string _tokenUrl = _keycloakBaseUrl +
            @"/auth/realms/sipgate-apps/protocol/openid-connect/token";

        private readonly string _loginUrl = _keycloakBaseUrl +
            @"/auth/realms/sipgate-apps/protocol/openid-connect/auth";

        private readonly string _logoutUrl = _keycloakBaseUrl +
            @"/auth/realms/sipgate-apps/protocol/openid-connect/logout";

        private const string GrantType = "authorization_code";
        private readonly string _state = RandomDataBase64Url(32);
        private const string ResponseType = "code";
        public readonly string redirectUri;
        private const string Scope = "faxlines:read sessions:fax:write history:read groups:faxlines:read offline_access contacts:read balance:read";

        private static int GetRandomUnusedPort()
        {
            var listener = new TcpListener(IPAddress.Loopback, 0);
            listener.Start();
            var port = ((IPEndPoint)listener.LocalEndpoint).Port;
            listener.Stop();
            return port;
        }

        public Authentication()
        {
            redirectUri = $"http://{IPAddress.Loopback}:{GetRandomUnusedPort()}/";
        }

        public HttpListener CreateLoginBrowserWindow()
        {
            // Creates an HttpListener to listen for requests on that redirect URI.
            var http = new HttpListener();
            http.Prefixes.Add(redirectUri);
            http.Start();

            // Creates the OAuth 2.0 authorization request.
            NameValueCollection queryString = System.Web.HttpUtility.ParseQueryString(string.Empty);
            queryString.Add("response_type", ResponseType);
            queryString.Add("client_id", _clientId);
            queryString.Add("redirect_uri", redirectUri);
            queryString.Add("state", _state);
            queryString.Add("scope", Scope);

            System.Diagnostics.Process.Start(_loginUrl + "?" + queryString);

            return http;
        }

        public void SendHttpResponse(HttpListenerContext context, bool success)
        {
            var response = context.Response;
            response.AddHeader("Content-Type", "text/html");

            var assembly = Assembly.GetExecutingAssembly();

            var resourceName = "SipgateFaxdrucker.Resources.RedirectionPage.html";
            if (!success)
            {
                resourceName = "SipgateFaxdrucker.Resources.RedirectionPageFailure.html";
            }


            using (var responseOutput = response.OutputStream)
            using (var stream = assembly.GetManifestResourceStream(resourceName))
            {
                stream?.CopyTo(responseOutput);
            }

            response.OutputStream.Close();
            FaxDruckerUtils.LogInformation("Sent response page to browser.");
        }


        /// <summary>
        /// Returns URI-safe data with a given input length.
        /// </summary>
        /// <param name="length">Input length (nb. output will be longer)</param>
        /// <returns></returns>
        private static string RandomDataBase64Url(uint length)
        {
            RNGCryptoServiceProvider rng = new RNGCryptoServiceProvider();
            byte[] bytes = new byte[length];
            rng.GetBytes(bytes);
            return Base64UrlencodeNoPadding(bytes);
        }

        /// <summary>
        /// Base64url no-padding encodes the given input buffer.
        /// </summary>
        /// <param name="buffer"></param>
        /// <returns></returns>
        private static string Base64UrlencodeNoPadding(byte[] buffer)
        {
            string base64 = Convert.ToBase64String(buffer);

            // Converts base64 to base64url.
            base64 = base64.Replace("+", "-");
            base64 = base64.Replace("/", "_");
            // Strips padding.
            base64 = base64.Replace("=", "");

            return base64;
        }

        public async Task<HttpListenerContext> AwaitResponse(HttpListener httpListener)
        {
            var context = await httpListener.GetContextAsync();
            return context;
        }

        public string ProcessResult(HttpListenerContext context)
        {
            // Checks for errors.
            if (context.Request.QueryString.Get("error") != null)
            {
                throw new AuthorizationException(context.Request.QueryString.Get("error"));
            }

            if (context.Request.QueryString.Get("code") == null
                || context.Request.QueryString.Get("state") == null)
            {
                MessageBox.Show("Malformed authorization response. " + context.Request.QueryString);
                throw new AuthorizationException(context.Request.QueryString.Get("error"));
            }

            // extracts the code
            var code = context.Request.QueryString.Get("code");
            var incomingState = context.Request.QueryString.Get("state");

            // Compares the receieved state to the expected value, to ensure that
            // this app made the request which resulted in authorization.
            if (incomingState != _state)
            {
                MessageBox.Show($"Received request with invalid state ({incomingState})");
                throw new AuthorizationException(context.Request.QueryString.Get("error"));
            }

            // Starts the code exchange at the Token Endpoint.
            return code;
        }

        public async Task<bool> PerformLogout(SipgateCredentials credentials)
        {
            try
            {
                var refreshToken = credentials.RefreshToken;
                var accessToken = credentials.AccessToken;

                if (refreshToken == "")
                {
                    FaxDruckerUtils.LogError($"Missing refresh token: {refreshToken}");
                    return false;
                }

                if (accessToken == "")
                {
                    FaxDruckerUtils.LogError($"Missing access token: {accessToken}");
                    return false;
                }

                HttpWebRequest tokenRequest = (HttpWebRequest)WebRequest.Create(_logoutUrl);

                NameValueCollection tokenRequestBody = System.Web.HttpUtility.ParseQueryString(string.Empty);
                tokenRequestBody.Add("client_id", _clientId);
                tokenRequestBody.Add("refresh_token", refreshToken);

                byte[] byteVersion = Encoding.ASCII.GetBytes($"{tokenRequestBody}");

                tokenRequest.Method = "POST";
                tokenRequest.ContentType = "application/x-www-form-urlencoded";
                tokenRequest.Accept = "application/json";
                tokenRequest.Headers["Authorization"] = $"Bearer {accessToken}";

                Stream stream = tokenRequest.GetRequestStream();
                await stream.WriteAsync(byteVersion, 0, byteVersion.Length);
                stream.Close();


                WebResponse tokenResponse = await tokenRequest.GetResponseAsync();

                var responseStream = tokenResponse.GetResponseStream();
                if (responseStream == null)
                {
                    return false;
                }
                using (StreamReader reader =
                    new StreamReader(responseStream))
                {
                    string responseText = await reader.ReadToEndAsync();
                    FaxDruckerUtils.LogInformation("responseText: " + responseText);
                    return true;

                }
            }
            catch (Exception e)
            {
                FaxDruckerUtils.LogCritical($"exception on logout: {e.Message}-{e.GetType()}");
                return false;
            }

        }

        public async Task<SipgateCredentials> PerformCodeExchange(string code)
        {
            HttpWebRequest tokenRequest = (HttpWebRequest)WebRequest.Create(_tokenUrl);

            try
            {
                SipgateCredentials sipgateCredentials = await ProcessCodeResponse(await PrepareCodeExchangeRequest(tokenRequest, code));

                return sipgateCredentials;
            }
            catch (WebException ex)
            {
                if (ex.Status == WebExceptionStatus.NameResolutionFailure)
                {
                    throw new NoInternetConnectionException();
                }

                if (ex.Status == WebExceptionStatus.ProtocolError)
                {
                    if (ex.Response is HttpWebResponse response)
                    {
                        var responseStream = response.GetResponseStream();
                        if (responseStream == null)
                        {
                            return null;
                        }

                        using (StreamReader reader = new StreamReader(responseStream))
                        {
                            string responseText = await reader.ReadToEndAsync();
                            throw new Exception(responseText);
                        }
                    }
                }

                FaxDruckerUtils.LogCritical($"could not refresh token:  {ex.Message} ({ex.Status})");
                return null;
            }
            catch (Exception e)
            {
                FaxDruckerUtils.LogCritical($"Could not perform code exchange: {e.StackTrace} {e.GetType()}");
                return null;
            }


        }

        private async Task<HttpWebRequest> PrepareCodeExchangeRequest(HttpWebRequest tokenRequest, string code)
        {
            NameValueCollection tokenRequestBody = System.Web.HttpUtility.ParseQueryString(string.Empty);
            tokenRequestBody.Add("code", code);
            tokenRequestBody.Add("client_id", _clientId);
            tokenRequestBody.Add("redirect_uri", redirectUri);
            tokenRequestBody.Add("scope", Scope);
            tokenRequestBody.Add("grant_type", GrantType);

            byte[] byteVersion = Encoding.ASCII.GetBytes($"{tokenRequestBody}");

            tokenRequest.ServerCertificateValidationCallback += (sender, certificate, chain, sslPolicyErrors) => true;
            tokenRequest.Method = "POST";
            tokenRequest.ContentType = "application/x-www-form-urlencoded";
            tokenRequest.Accept = "application/json";
            tokenRequest.ContentLength = byteVersion.Length;

            try
            {
                Stream stream = tokenRequest.GetRequestStream();
                await stream.WriteAsync(byteVersion, 0, byteVersion.Length);
                stream.Close();
            }
            catch (Exception e)
            {
                FaxDruckerUtils.LogCritical("Could not perform code exchange:" + e.Message);
                return null;
            }

            return tokenRequest;
        }

        private async Task<SipgateCredentials> ProcessCodeResponse(HttpWebRequest tokenRequest)
        {
            WebResponse tokenResponse = await tokenRequest.GetResponseAsync();

            var responseStream = tokenResponse.GetResponseStream();
            if (responseStream == null)
            {
                return null;
            }

            using (StreamReader reader =
                new StreamReader(responseStream))
            {
                string responseText = await reader.ReadToEndAsync();
                Dictionary<string, string> tokenEndpointDecoded =
                    JsonConvert.DeserializeObject<Dictionary<string, string>>(responseText);
                return new SipgateCredentials(tokenEndpointDecoded["access_token"], tokenEndpointDecoded["refresh_token"]);
            }
        }

        public async Task<SipgateCredentials> RefreshAccessToken(string refreshToken)
        {
            HttpWebRequest tokenRequest = (HttpWebRequest)WebRequest.Create(_tokenUrl);
            try
            {
                await PrepareTokenRequest(tokenRequest, refreshToken);

                SipgateCredentials credentials = await ProcessRefreshReponse(tokenRequest);
                return credentials;
            }
            catch (WebException wex)
            {

                if (wex.Status == WebExceptionStatus.NameResolutionFailure || wex.Status == WebExceptionStatus.ConnectFailure)
                {
                    throw new NoInternetConnectionException();
                }

                if (wex.Status == WebExceptionStatus.ProtocolError)
                {
                    if (wex.Response is HttpWebResponse response)
                    {
                        var responseStream = response.GetResponseStream();
                        if (responseStream == null)
                        {
                            return null;
                        }

                        using (StreamReader reader = new StreamReader(responseStream))
                        {
                            string responseText = await reader.ReadToEndAsync();
                            throw new InvalidRefreshTokenException(responseText);
                        }
                    }
                }

                FaxDruckerUtils.LogCritical($"could not refresh token: {wex.Status}");
                return null;
            }
            catch (Exception e)
            {
                FaxDruckerUtils.LogCritical($"could not refresh token: {e.Message} ({e.GetType()})");
                return null;
            }
        }

        private async Task<bool> PrepareTokenRequest(HttpWebRequest tokenRequest, string refreshToken)
        {
            NameValueCollection tokenRequestBody = System.Web.HttpUtility.ParseQueryString(string.Empty);
            tokenRequestBody.Add("refresh_token", refreshToken);
            tokenRequestBody.Add("client_id", _clientId);
            tokenRequestBody.Add("grant_type", "refresh_token");

            byte[] byteVersion = Encoding.ASCII.GetBytes("" + tokenRequestBody);

            tokenRequest.ServerCertificateValidationCallback += (sender, certificate, chain, sslPolicyErrors) => true;
            tokenRequest.Method = "POST";
            tokenRequest.ContentType = "application/x-www-form-urlencoded";
            tokenRequest.Accept = "application/json";
            tokenRequest.ContentLength = byteVersion.Length;

            Stream stream = null;
            try
            {
                stream = tokenRequest.GetRequestStream();
                await stream.WriteAsync(byteVersion, 0, byteVersion.Length);
                stream.Close();
                return true;
            }
            catch (WebException wex)
            {
                FaxDruckerUtils.LogCritical($"get request stream failed:{wex}");
                if (stream != null) stream.Close();
                throw;
            }
        }

        private async Task<SipgateCredentials> ProcessRefreshReponse(HttpWebRequest tokenRequest)
        {
            WebResponse tokenResponse = await tokenRequest.GetResponseAsync();

            var responseStream = tokenResponse.GetResponseStream();
            if (responseStream == null)
            {
                return null;
            }

            using (StreamReader reader =
                new StreamReader(responseStream))
            {
                string responseText = await reader.ReadToEndAsync();
                Dictionary<string, string> tokenEndpointDecoded =
                    JsonConvert.DeserializeObject<Dictionary<string, string>>(responseText);
                return new SipgateCredentials(tokenEndpointDecoded["access_token"], tokenEndpointDecoded["refresh_token"]);
            }
        }
    }
}
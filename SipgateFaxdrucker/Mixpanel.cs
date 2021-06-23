using Mixpanel;
using System;
using System.Diagnostics;
using System.Net.Http;
using System.Reflection;
using System.Threading.Tasks;
using SipgateFaxdrucker.Properties;
using SipgateFaxdrucker.SipgateAPI.Models;

namespace SipgateFaxdrucker
{
    class Mixpanel
    {
        static readonly HttpClient HttpClient = new HttpClient();       
        private readonly UserinfoResponse _userinfo;
        private MixpanelClient _mc;

        public Mixpanel(UserinfoResponse userInfo)
        {
            _userinfo = userInfo;
            InitMixpanel();
        }

        private void InitMixpanel()
        {
            var token = Properties.Settings.Default.MixpanelToken;
#if DEBUG
            token = Properties.Settings.Default.MixpanelTokenDebug;
#endif
            try
            {
                MixpanelConfig.Global.ErrorLogFn = LogErrors;
                MixpanelConfig.Global.HttpPostFn = CustomPostFn;

                object superProperties = new
                {
                    _userinfo.Product,
                    _userinfo.Domain,
                    Admin = _userinfo.IsAdmin,
                    Testaccount = _userinfo.IsTestAccount,
                    Client = Settings.Default.MixpanelClient,
                    VersionNumber = FileVersionInfo.GetVersionInfo(Assembly.GetExecutingAssembly().Location).ProductVersion,
                    DistinctId = Utils.CreateSha256Hash($"{_userinfo.MasterSipId}{_userinfo.Sub}"),
                    account_id = Utils.CreateSha256Hash(_userinfo.MasterSipId),
                    OS = Environment.OSVersion.VersionString,
                    Is64Bit= Environment.Is64BitOperatingSystem.ToString(),
                };

                MixpanelConfig config = new MixpanelConfig
                {
                    MixpanelPropertyNameFormat = MixpanelPropertyNameFormat.TitleCase
                };

                _mc = new MixpanelClient(token, config, superProperties);
                _mc.PeopleSet(new
                {
                    Product = _userinfo.Product,
                    ProductDomain = _userinfo.Domain,
                    Admin = _userinfo.IsAdmin,
                    Testaccount = _userinfo.IsTestAccount,
                });

            }
            catch (Exception e)
            {
                Utils.LogCritical(e.Message);
            }
        }

        public async Task<bool> TrackPageView(string path)
        {
            object properties = new
            {
                PagePath = path
            };

            return await _mc.TrackAsync("View Page", properties);
        }

        private bool CustomPostFn(string url, string formData)
        {
            // necessary workaround to post to the correct host. Not configurable in Mixpanel Config or per parameters
            var correctApiHost = url.Replace("api.mixpanel.com", "api-eu.mixpanel.com");

            return CustomPostFnAsync(correctApiHost, formData).GetAwaiter().GetResult();
        }

        private async Task<bool> CustomPostFnAsync(string url, string formData)
        {
            HttpResponseMessage responseMessage =
                await HttpClient.PostAsync(url, new StringContent(formData)).ConfigureAwait(false);
            if (!responseMessage.IsSuccessStatusCode)
            {
                return false;
            }

            string responseContent = await responseMessage.Content.ReadAsStringAsync().ConfigureAwait(false);
            return responseContent == "1";
        }


        private void LogErrors(string error, Exception exception)
        {
            Utils.LogCritical($"{error} - {exception.Message}");
        }
    }
}

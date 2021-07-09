using Microsoft.Rest;
using PhoneNumbers;
using SipgateFaxdrucker.SipgateAPI;
using SipgateFaxdrucker.SipgateAPI.Models;
using System;
using System.Threading.Tasks;
using System.Windows;

namespace SipgateFaxdrucker
{
    public class FaxManager
    {
        public string FileName { get; set; }
        public PhoneNumber TargetNumber { get; set; }

        public async Task<FaxlinesResponse> GetFaxLineAsync(SipgateApi api)
        {
            try
            {
                FaxDruckerUtils.LogInformation("API: Get user info...", 1);
                UserinfoResponse userinfo = await api.UserinfoAsync();
                if (userinfo != null)
                {
                    FaxDruckerUtils.LogInformation($"API: Got user info: {userinfo.Sub}");
                }
                else
                {
                    FaxDruckerUtils.LogInformation("API: Error fetching user info: ");
                    return null;
                }

                FaxDruckerUtils.LogInformation("API: Get fax lines info...");
                FaxlinesResponse faxlines = await api.GetUserFaxlinesAsync(userinfo.Sub);
                FaxDruckerUtils.LogInformation($"API: Got fax lines: {faxlines.Items.Count}");

                return faxlines;
            }
            catch (Exception ex)
            {
                FaxDruckerUtils.LogCritical($"API: Error fetching user info: {ex.Message}");
                return null;
            }
        }

        public async Task<GroupFaxlinesResponse> GetGroupFaxLineAsync(SipgateApi api)
        {
            try
            {

                FaxDruckerUtils.LogInformation("API: Get user info...", 2);
                UserinfoResponse userinfo = await api.UserinfoAsync();
                FaxDruckerUtils.LogInformation($"API: Got user info: {userinfo.Sub}");

                FaxDruckerUtils.LogInformation("API: Get group fax lines info...");
                GroupFaxlinesResponse faxlines = await api.GetGroupFaxlinesForUserAsync(userinfo.Sub);
                FaxDruckerUtils.LogInformation($"API: Got group fax lines: {faxlines.Items.Count}");

                return faxlines;
            }
            catch (Exception e)
            {
                MessageBox.Show(e.Message, "get groupfaxlines");
                return null;
            }
        }

        public async Task<string> SendFileWithSipgateApi(string faxlineId, SipgateApi api)
        {
            var base64Content = await Task.Run(() => FaxDruckerUtils.ConvertPdfToBase64(FileName));

            FaxDruckerUtils.LogInformation("Calling API...:");

            try
            {
                if (faxlineId == null)
                {
                    FaxDruckerUtils.LogError("Fax could not be sent: No faxline found for logged in user");
                    return null;
                }

                FaxDruckerUtils.LogInformation("Filename: " + FileName);
                var faxDocumentName = $"fax_{DateTime.Now:dd-MM-yyyy-HH-mm}.pdf";
                FaxDruckerUtils.LogInformation("faxName: " + faxDocumentName);
                SendFaxRequest request = new SendFaxRequest
                {
                    FaxlineId = faxlineId,
                    Filename = faxDocumentName,
                    Recipient = FaxDruckerUtils.E164TargetNumber(TargetNumber),
                    Base64Content = base64Content,
                };

                FaxDruckerUtils.LogInformation("Response received.");

                SendFaxSessionResponse response = await api.SendFaxAsync(request);
                if (response.StatusCode == System.Net.HttpStatusCode.OK)
                {
                    FaxDruckerUtils.LogInformation($"Success! SessionID: {response.SessionId}");
                    return response.SessionId;
                }
                else
                {
                    FaxDruckerUtils.LogError("Failed! Response code was: " + response.StatusCode);
                    return null;
                }
            }
            catch (ValidationException)
            {
                throw;
            }
            catch (Exception ex)
            {
                FaxDruckerUtils.LogError($"Error while sending fax: {ex.GetType()} {ex.Message} {ex.StackTrace}");
                return null;
            }
        }
    }
}
using Microsoft.Rest;
using System;
using System.Windows;
using System.Threading.Tasks;
using PhoneNumbers;
using SipgateFaxdrucker.SipgateAPI;
using SipgateFaxdrucker.SipgateAPI.Models;

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
                Utils.LogInformation("API: Get user info...", 1);
                UserinfoResponse userinfo = await api.UserinfoAsync();
                if (userinfo != null)
                {
                    Utils.LogInformation($"API: Got user info: {userinfo.Sub}");
                }
                else
                {
                    Utils.LogInformation("API: Error fetching user info: ");
                    return null;
                }

                Utils.LogInformation("API: Get fax lines info...");
                FaxlinesResponse faxlines = await api.GetUserFaxlinesAsync(userinfo.Sub);
                Utils.LogInformation($"API: Got fax lines: {faxlines.Items.Count}");

                return faxlines;
            }
            catch (Exception ex)
            {
                Utils.LogCritical($"API: Error fetching user info: {ex.Message}");
                return null;
            }
        }

        public async Task<GroupFaxlinesResponse> GetGroupFaxLineAsync(SipgateApi api)
        {
            try
            {

                Utils.LogInformation("API: Get user info...", 2);
                UserinfoResponse userinfo = await api.UserinfoAsync();
                Utils.LogInformation($"API: Got user info: {userinfo.Sub}");

                Utils.LogInformation("API: Get group fax lines info...");
                GroupFaxlinesResponse faxlines = await api.GetGroupFaxlinesForUserAsync(userinfo.Sub);
                Utils.LogInformation($"API: Got group fax lines: {faxlines.Items.Count}");

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
            var base64Content = await Task.Run(() => Utils.ConvertPdfToBase64(FileName));

            Utils.LogInformation("Calling API...:");

            try
            {
                if (faxlineId == null)
                {
                    Utils.LogError("Fax could not be sent: No faxline found for logged in user");
                    return null;
                }

                Utils.LogInformation("Filename: " + FileName);
                var faxDocumentName = $"fax_{DateTime.Now:dd-MM-yyyy-HH-mm}.pdf";
                Utils.LogInformation("faxName: " + faxDocumentName);
                SendFaxRequest request = new SendFaxRequest
                {
                    FaxlineId = faxlineId,
                    Filename = faxDocumentName,
                    Recipient = Utils.E164TargetNumber(TargetNumber),
                    Base64Content = base64Content,
                };

                Utils.LogInformation("Response received.");

                SendFaxSessionResponse response = await api.SendFaxAsync(request);
                if (response.StatusCode == System.Net.HttpStatusCode.OK)
                {
                    Utils.LogInformation($"Success! SessionID: {response.SessionId}");
                    return response.SessionId;
                }
                else
                {
                    Utils.LogError("Failed! Response code was: " + response.StatusCode);
                    return null;
                }
            }
            catch (ValidationException)
            {
                throw;
            }
            catch (Exception ex)
            {
                Utils.LogError($"Error while sending fax: {ex.GetType()} {ex.Message} {ex.StackTrace}");
                return null;
            }
        }
    }

    public class SelectableFaxline
    {
        public SelectableFaxline(FaxlineResponse faxline)
        {
            Id = faxline.Id;
            Alias = faxline.Alias;
        }

        public SelectableFaxline(GroupFaxlineResponse faxline)
        {
            Id = faxline.Id;
            Alias = faxline.Alias;
        }

        public string Id { get; }

        public string Alias { get; }



    }
}
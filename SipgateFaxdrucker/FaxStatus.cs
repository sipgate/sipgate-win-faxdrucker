using System;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Windows.Media;
using System.Windows.Threading;
using System.Windows;
using SipgateFaxdrucker.SipgateAPI;
using SipgateFaxdrucker.SipgateAPI.Models;

namespace SipgateFaxdrucker
{
    public class FaxStatus
    {
        private static Dictionary<string, FaxStatusDetail> _status = new Dictionary<string, FaxStatusDetail>
        {
            {"PENDING", new FaxStatusDetail("Warten auf Versand..." , Brushes.DarkOrange, false) },
            {"SENDING", new FaxStatusDetail("Fax wird versendet...", Brushes.DarkOrange, false)},
            {"FAILED", new FaxStatusDetail("Versand fehlgeschlagen", Brushes.Red, true)},
            {"SENT", new FaxStatusDetail("Fax erfolgreich versendet!", Brushes.Green, true)},
        };

        private readonly string _sessionId;
        private SipgateApi _api;

        public FaxStatus(string sessionId, SipgateApi sipgateApi)
        {
            this._sessionId = sessionId;
            this._api = sipgateApi;
        }

        private async Task<string> CheckStatus()
        {
            HistoryEntryResponse historyResponse = await _api.GetHistoryByIdAsync(_sessionId);

            if (historyResponse == null)
            {
                return null;
            }

            return historyResponse.FaxStatusType;
        }

        public void PollForStatus(SipgateForm form)
        {
            var timer = new DispatcherTimer {Interval = TimeSpan.FromSeconds(1)};

            timer.Tick += async (sender, e) =>
            {
                DispatcherTimer originalTimer = (DispatcherTimer)sender;

                string status;
                try
                {
                    status = await CheckStatus();
                }
                catch (Exception ex)
                {
                    form.TextSendStatus.Text = "Abfrage des Status fehlgeschlagen";
                    Utils.LogCritical($"CheckStatus failed: {ex.Message}");
                    originalTimer.Stop();
                    return;
                }

                if (status != null)
                {
                   FaxStatusDetail faxStatusDetail = FaxStatus._status[status];
                    if (faxStatusDetail != null)
                    {
                        form.TextSendStatus.Text = faxStatusDetail.Translation;
                        form.TextSendStatus.Foreground = faxStatusDetail.Color;

                        if (faxStatusDetail.IsFinal)
                        {
                            form.BtnClose.Visibility = Visibility.Visible;
                            form.BtnMinimize.Visibility = Visibility.Collapsed;
                            originalTimer.Stop();
                        }
                    }
                }
            };
            timer.Start();
        }
    }

    public class FaxStatusDetail
    {
        public string Translation { get; }
        public SolidColorBrush Color { get; }
        public bool IsFinal { get; }

        public FaxStatusDetail(string translation, SolidColorBrush color, bool isFinal)
        {
            this.Translation = translation;
            this.Color = color;
            this.IsFinal = isFinal;
        }
    }
}
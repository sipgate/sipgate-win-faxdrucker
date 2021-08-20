using AutoUpdaterDotNET;
using Microsoft.Rest;
using PhoneNumbers;
using SipgateFaxdrucker.GhostScript;
using SipgateFaxdrucker.Properties;
using SipgateFaxdrucker.SipgateAPI;
using SipgateFaxdrucker.SipgateAPI.Models;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Threading;
using static SipgateFaxdrucker.SipgateAPI.SipgateApi;
using SipgateFaxdrucker.Utils.Auth;

namespace SipgateFaxdrucker
{
   /// <summary>
   /// Interaktionslogik für SipgateForm.xaml
   /// </summary>
   public partial class SipgateForm : Window, IDisposable, INotifyPropertyChanged
   {
      public enum FormPage
      {
         Login,
         TargetNumber,
         SendingStatus
      }
      private const int MaxContactsSupported = 1000;
      private const double FaxCostsInCent = 1.79;
      private const string ErrorColor = "#c90c2f";
      private string _filename;
      public static CredentialManager credentialManager;
      public PhoneNumber phonenumber;
      public FaxManager faxManager;

      private SimpleDelegateCommand _zoomInCommand;
      public SimpleDelegateCommand ZoomInCommand => _zoomInCommand;

      private SimpleDelegateCommand _zoomOutCommand;
      public SimpleDelegateCommand ZoomOutCommand => _zoomOutCommand;    

#if DEBUG
  //    private readonly string _apiUrl = @"https://api.dev.sipgate.com/v2/";
#else
       
#endif
      private readonly string _apiUrl = Settings.Default.ApiBaseUrl + @"/v2/";
      public static Mixpanel mixpanel;

      private string _contactInfoText = $"Suche unterstützt maximal {MaxContactsSupported} Fax-Kontakte.";
      public string ContactInfoText => _contactInfoText;

      private Thickness _errorTextMargin;

      public Thickness ErrorTextMargin
      {
         get => _errorTextMargin;
         set
         {
            _errorTextMargin = value;
            OnPropertyChanged();
         }
      }

      public event PropertyChangedEventHandler PropertyChanged;
      private void OnPropertyChanged([CallerMemberName] string propertyName = null)
      {
         PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
      }

      public static SipgateForm Instance
      {
         get; private set;
      }

      public SipgateForm()
      {
         Instance = this;
         credentialManager = new CredentialManager();
         InitializeComponent();
         InitializeCommand();
      }

      #region EventHandler 

      private async void Window_Loaded(object sender, RoutedEventArgs e)
      {
         try
         {
            FaxDruckerUtils.LogCritical("Open Update Window");
            AutoUpdater.OpenDownloadPage = true;
            AutoUpdater.Start(Settings.Default.UpdateCheckUrl);
         }
         catch (Exception uex)
         {
            FaxDruckerUtils.LogCritical($"Error opening Updater: {uex.Message}");
            return;
         }

         try
         {
            if (!Debugger.IsAttached)
            {
               if (!InitializeGhostscript())
               {
                  FaxDruckerUtils.LogError("Printing file failed because it was too large");

                  MessageBox.Show("Die gedruckte Datei ist zu groß, um gesendet zu werden. Bitte versuchen Sie es mit einer kleineren Datei erneut.", "sipgate Faxdrucker Fehler", MessageBoxButton.OK, MessageBoxImage.Error);
                  Close();

                  return;
               }
            }

            bool successfullyRefreshed = await RefreshToken();
            if (!successfullyRefreshed)
            {
               FaxDruckerUtils.LogWarning("Refreshing token failed");
               LogoutUser();
            }
            else
            {
               FaxDruckerUtils.LogInformation("RefreshToken: \n" + credentialManager.GetCredentials().RefreshToken);
               ShowPage(FormPage.TargetNumber);
            }

         }
         catch (Exception ex)
         {
            FaxDruckerUtils.LogCritical($"ERROR: {ex.Message}");
         }

      }

      private void ZoomButton_Clicked(string direction)
      {
         if (direction == "IN")
         {
            MainGrid.LayoutTransform = new ScaleTransform(2.0, 2.0);
            MaxWidth = 700;
         }
         else
         {
            MainGrid.LayoutTransform = new ScaleTransform(1.0, 1.0);
            MaxWidth = 350;
         }
      }

      private void btnMinimize_Click(object sender, RoutedEventArgs e)
      {
         WindowState = WindowState.Minimized;
      }

      private void btnClose_Click(object sender, RoutedEventArgs e)
      {
         Close();
      }

      private void Window_KeyDown(object sender, KeyEventArgs e)
      {
         if (e.Key == Key.Enter && FaxView.IsSendButtonEnabled && FaxView.IsVisible)
         {
            HandleSendFax();
         }
      }

      #endregion

      #region functionality

      private bool InitializeGhostscript()
      {
         var MaxFileSize = 9437184;//9MB

         GhostScriptRunner runner = new GhostScriptRunner();
         _filename = runner.Print(Path.GetTempFileName());

         double sizeInfoInBytes = FaxDruckerUtils.GetFileSize(_filename);
         FaxDruckerUtils.LogCritical($"Ghostscript: File printed with size: {FaxDruckerUtils.GetFormattedFileSize(sizeInfoInBytes)} ({_filename})");

         if (sizeInfoInBytes > MaxFileSize)
         {
            FaxDruckerUtils.LogError($"File too large to be send: {(int)sizeInfoInBytes} byte");
            return false;
         }

         return true;
      }

      public async void LogoutUser(bool keycloakLogout = false, string errorMessage = "")
      {
         if (keycloakLogout && credentialManager.IsLoggedIn())
         {
            var auth = new Authentication();
            bool loggedOutSuccessfully = await auth.PerformLogout(credentialManager.GetCredentials());
            if (!loggedOutSuccessfully)
            {
               FaxDruckerUtils.LogWarning("User not logged out properly");
            }
         }

         credentialManager.RemoveCredentials();
         ShowPage(FormPage.Login);

         LoginView.showErrorOnLoginView(errorMessage);
      }   

      public async void HandleSendFax()
      {
         ShowPage(FormPage.SendingStatus);

         BtnMinimize.Visibility = Visibility.Visible;
         BtnClose.Visibility = Visibility.Collapsed;

         TextSendStatus.Text = "Übertragung gestartet...";

         try
         {
            if (!await RefreshToken())
            {
               FaxDruckerUtils.LogWarning("Error Refreshing Token before Sending fax: The Token was no longer valid");
               LogoutUser(false, "Fehler bei den Zugangsdaten, bitte loggen Sie sich wieder ein");
               return;
            }
         }
         catch (NoInternetConnectionException nex)
         {
            FaxDruckerUtils.LogCritical($"Could not refresh because loginserver could not be reached (e.g. no internet): {nex.Message}");
            LogoutUser(false, "Bitte stellen Sie sicher, dass Sie eine Verbindung zum Internet haben.");
            return;
         }

         try
         {
            TextSendStatus.Text = "Dokument wird an sipgate übermittelt...";

            faxManager.FileName = _filename;
            faxManager.TargetNumber = GetPhoneNumber();

            if (FaxView.selectedFaxlineId == null)
            {
               FaxDruckerUtils.LogWarning("No faxline available");
               return;
            }

            var sessionId = await faxManager.SendFileWithSipgateApi(FaxView.selectedFaxlineId, GetApiClient());

            if (sessionId != null)
            {
               TextSendStatus.Text = "Dokument erfolgreich übermittelt!";
               TextSendStatus.Foreground = Brushes.DarkGreen;

               BtnMinimize.Visibility = Visibility.Visible;
               BtnClose.Visibility = Visibility.Collapsed;

               FaxStatus fs = new FaxStatus(sessionId, GetApiClient());
               fs.PollForStatus(this);
            }
            else
            {
               FaxDruckerUtils.LogWarning("Sending failed: Api returned no sessionId", 400);
               TextSendStatus.Text =
                   "Übermittlung fehlgeschlagen. Bitte versuchen Sie es später erneut. (F400)";

               ShowSendingFailedMessage();
            }
         }
         catch (ValidationException vex)
         {
            FaxDruckerUtils.LogCritical($"Sending failed because of ValidationException: {vex.Rule}", 413);
            TextSendStatus.Text = vex.Rule == ValidationRules.MaxLength
                ? "Übermittlung fehlgeschlagen. Datei ist größer als 9MB. (F413)"
                : "Übermittlung fehlgeschlagen. Datei ist unzulässig. (F415)";

            ShowSendingFailedMessage();
         }
         catch (Exception exception)
         {
            FaxDruckerUtils.LogCritical($"Exception while sending: {exception.Message}", 500);
            TextSendStatus.Text = "Übermittlung fehlgeschlagen. Bitte versuchen Sie es später erneut. (F500)";
            ShowSendingFailedMessage();
         }
      }

      private void ShowSendingFailedMessage()
      {
         Color errorColor = (Color)ColorConverter.ConvertFromString(ErrorColor);
         TextSendStatus.Foreground =
             new SolidColorBrush(errorColor);
         BtnClose.Visibility = Visibility.Visible;
         BtnMinimize.Visibility = Visibility.Collapsed;
      }

      private PhoneNumber GetPhoneNumber()
      {
         if (phonenumber == null && FaxView.SelectedContact != null)
         {
            var numberValidationResult = FaxDruckerUtils.GetNumberValidationResult(FaxView.SelectedContact.Number);
            if (numberValidationResult.wasSuccessful)
            {
               return numberValidationResult.phonenumber;
            }
         }

         return phonenumber;
      }

      private async Task<bool> RefreshToken()
      {
         FaxDruckerUtils.LogInformation("Will Refresh");
         if (credentialManager == null || credentialManager.GetCredentials() == null)
         {
            FaxDruckerUtils.LogError("No credential Manager found");
            return false;
         }

         try
         {
            var auth = new Authentication();
            SipgateCredentials sipgateCredentials =
                await auth.RefreshAccessToken(credentialManager.GetCredentials().RefreshToken);
            if (sipgateCredentials == null)
            {
               FaxDruckerUtils.LogWarning("Could not refresh token due to missing credentials");
               return false;
            }

            credentialManager.SaveCredentials(sipgateCredentials);
            return true;
         }
         catch (InvalidRefreshTokenException iex)
         {
            FaxDruckerUtils.LogCritical($"Invalid refresh token: {iex.Message}");
            return false;
         }
         catch (NoInternetConnectionException)
         {
            throw;
         }
         catch (Exception ex)
         {
            FaxDruckerUtils.LogCritical($"Unknown error during token refresh: {ex.Message}");
            return false;
         }
      }

      public SipgateApi GetApiClient()
      {
         return new SipgateApi(new Uri(_apiUrl),
             new TokenCredentials(credentialManager.GetCredentials().AccessToken));
      }

      private void InitializeCommand()
      {
         DataContext = this;
         _zoomInCommand = new SimpleDelegateCommand(x => ZoomButton_Clicked("IN"))
         {
            GestureKey = Key.OemPlus,
            GestureModifier = ModifierKeys.Control
         };

         _zoomOutCommand = new SimpleDelegateCommand(x => ZoomButton_Clicked("OUT"))
         {
            GestureKey = Key.OemMinus,
            GestureModifier = ModifierKeys.Control
         };

      }

      private List<Contact> MapContactReponseToContactObjects(ContactResponse contactResponse)
      {
         List<Contact> contactList = new List<Contact>();
         string[] faxNumbers = contactResponse.Numbers.Where(numberObject => numberObject.Type.Contains("fax")).Select(numberObject => numberObject.Number).ToArray();

         if (!faxNumbers.Any())
         {
            return contactList;
         }

         var organization = "";
         if (contactResponse.Organization.Any())
         {
            if (contactResponse.Organization[0].Any())
            {
               organization = contactResponse.Organization[0][0];
            }
         }

         foreach (string faxNumber in faxNumbers)
         {
            var contact = new Contact(contactResponse.Name, faxNumber, organization);

            contactList.Add(contact);
         }

         FaxDruckerUtils.LogInformation($"Converted {contactList.Count} contactResponses to contactObjects");
         return contactList;
      }

      #endregion


      #region manipulateUI
      public void ShowPage(FormPage page)
      {
         if (credentialManager == null || !credentialManager.IsLoggedIn())
         {
            LoginView.Visibility = Visibility.Visible;
            FaxView.Visibility = Visibility.Collapsed;
            StatusView.Visibility = Visibility.Collapsed;
            FaxDruckerUtils.LogInformation("not authorized. staying at login");
            return;
         }

         LoginView.Visibility = page == FormPage.Login ? Visibility.Visible : Visibility.Collapsed;
         FaxView.Visibility = page == FormPage.TargetNumber ? Visibility.Visible : Visibility.Collapsed;
         StatusView.Visibility = page == FormPage.SendingStatus ? Visibility.Visible : Visibility.Collapsed;

         if (mixpanel != null)
         {
            if (page == FormPage.Login)
            {
               FaxDruckerUtils.LogInformation("Sending PageView Event to Mixpanel");
               _ = mixpanel.TrackPageView("/LoginView");

            }
            else if (page == FormPage.SendingStatus)
            {
               FaxDruckerUtils.LogInformation("Sending PageView Event to Mixpanel");
               _ = mixpanel.TrackPageView("/StatusView");

            }
         }

      }

      public async Task SetEligibleFaxlines(SipgateApi apiClient)
      {
         await FaxView.SetEligibleFaxlines(apiClient);
      }
      #endregion

      public void Dispose()
      {
         // Suppress finalization.
         GC.SuppressFinalize(this);
      } 
   }
}
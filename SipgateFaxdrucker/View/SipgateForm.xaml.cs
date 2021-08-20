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
      private string _selectedFaxlineId;
      private FaxManager _faxManager;
      private PhoneNumber _phonenumber;

      private SimpleDelegateCommand _zoomInCommand;
      public SimpleDelegateCommand ZoomInCommand => _zoomInCommand;

      private SimpleDelegateCommand _zoomOutCommand;
      public SimpleDelegateCommand ZoomOutCommand => _zoomOutCommand;

      ObservableCollection<SelectableFaxline> _faxlinesItem = new ObservableCollection<SelectableFaxline>();
      public ObservableCollection<SelectableFaxline> Faxlines => _faxlinesItem;

#if DEBUG
  //    private readonly string _apiUrl = @"https://api.dev.sipgate.com/v2/";
#else
       
#endif
      private readonly string _apiUrl = Settings.Default.ApiBaseUrl + @"/v2/";
      private Mixpanel _mixpanel;
      private UserinfoResponse _userinfo;

      readonly ObservableCollection<Contact> _contactsCollection = new ObservableCollection<Contact>();
      readonly ObservableCollection<Contact> _filteredContactsCollection = new ObservableCollection<Contact>();

      private bool _isFaxlineSelected;
      private bool _isValidPhonenumber;
      private bool _isBalanceOkay = true;
      private bool _isSendButtonEnabled;
      public bool IsSendButtonEnabled
      {
         get => _isSendButtonEnabled;
         set
         {
            if (_isSendButtonEnabled != value)
            {
               _isSendButtonEnabled = value;
               OnPropertyChanged();
            }
         }
      }

      private string _contactInfoText = $"Suche unterstützt maximal {MaxContactsSupported} Fax-Kontakte.";
      public string ContactInfoText => _contactInfoText;

      private bool _hasTooManyContacts = false;
      public bool HasTooManyContacts
      {
         get => _hasTooManyContacts;
         set
         {
            _hasTooManyContacts = value;
            OnPropertyChanged();
         }
      }

      private CollectionView _contactsView;
      public CollectionView ContactsView
      {
         get => _contactsView;
         set
         {
            _contactsView = value;
            OnPropertyChanged();
         }
      }

      private Contact _selectedContact;
      public Contact SelectedContact
      {
         get => _selectedContact;
         set
         {
            if (value != null)
            {
               _selectedContact = value;
               OnPropertyChanged();
               SearchText = _selectedContact.Number;
            }
         }
      }

      private string _searchText = "";
      public string SearchText
      {
         get => _searchText;
         set
         {
            if (value == null)
            {
               return;
            }
            _searchText = value;
            OnPropertyChanged();
            if (SelectedContact != null && _searchText == SelectedContact.Number)
            {
               return;
            }

            TargetNumberComboBox.IsDropDownOpen = true;
            ContactsView.Refresh();
         }
      }

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

      private void LogoutButton_Click(object sender, RoutedEventArgs e)
      {
         LogoutUser(true);
      }

      private void BtnSend_Click(object sender, RoutedEventArgs e)
      {
         HandleSendFax();
      }

      private void ComboBox_Selected(object sender, SelectionChangedEventArgs args)
      {
         if (args.AddedItems.Count < 1)
         {
            CheckShouldEnable(false, null, null);
            FaxDruckerUtils.LogError("No faxline item to be selected");
            return;
         }

         if (args.AddedItems[0] is SelectableFaxline faxline)
         {
            _selectedFaxlineId = faxline.Id;
            CheckShouldEnable(true, null, null);

            FaxDruckerUtils.LogInformation($"Selected Faxline: {faxline.Id}");
         }
         else
         {
            FaxDruckerUtils.LogError("Error casting selected Item");
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

      private async void FaxView_IsVisibleChanged(object sender, DependencyPropertyChangedEventArgs e)
      {

         StackPanel faxView = (StackPanel)sender;

         if (faxView.Visibility == Visibility.Visible)
         {
            SipgateApi apiClient = GetApiClient();

            FaxDruckerUtils.LogInformation("API: Get user info...", 3);

            try
            {
               _userinfo = await apiClient.UserinfoAsync();
               if (_userinfo != null)
               {
                  FaxDruckerUtils.LogInformation($"API: Got user info: {_userinfo.Sub} {_userinfo.Sub}");
               }
               else
               {
                  FaxDruckerUtils.LogCritical("API: userinfo came back empty. Exiting");
                  LogoutUser(false, "Es gab eine fehlerhafte Antwort vom Server.");
                  return;
               }
            }
            catch (Exception ex)
            {
               FaxDruckerUtils.LogCritical($"Encountered error while initializing FaxView: {ex.Message} ({ex.StackTrace})");
               return;
            }

            try
            {
               InitContacts(apiClient);

            }
            catch (Exception ex)
            {
               FaxDruckerUtils.LogCritical($"Encountered error while initializing contacts {ex.Message} ({ex.StackTrace})");
               LogoutUser(false, "Beim Laden der Kontakte ist ein Fehler aufgetreten.");
               return;
            }

            try
            {
               FaxDruckerUtils.LogInformation("Initializing Mixpanel");

               _mixpanel = new Mixpanel(_userinfo);

               var success = await _mixpanel.TrackPageView("/FaxView");
               FaxDruckerUtils.LogInformation(success
                   ? "Mixpanel: successfully send page view event."
                   : "Mixpanel: failed to send page view event.");
            }
            catch (Exception ex)
            {
               FaxDruckerUtils.LogCritical($"Mixpanel: {ex.Message}");
            }


            try
            {
               FaxDruckerUtils.LogInformation("API: Start fetching eligible faxlines");
               await SetEligibleFaxlines(GetApiClient());
               FaxDruckerUtils.LogInformation("API: Done fetching eligible faxlines");
            }
            catch (Exception ex)
            {
               FaxDruckerUtils.LogCritical($"Error in Combobox loaded: {ex.Message} {ex.StackTrace}");
            }

            try
            {
               FaxDruckerUtils.LogInformation("API: Start fetching account balance");
               BalanceResponse balanceResponse = await apiClient.BalanceAsync();

               if (balanceResponse != null && balanceResponse.Amount.HasValue)
               {
                  FaxDruckerUtils.LogInformation($"API: Done fetching account balance: {balanceResponse.Amount}{balanceResponse.Currency}");

                  var balanceInCent = (double)balanceResponse.Amount.Value / 100;
                  if (!(balanceInCent < FaxCostsInCent) || balanceResponse.Currency != "EUR")
                  {
                     return;
                  }

                  FaxDruckerUtils.LogError($"Not enough money! {balanceInCent}ct");
                  CheckShouldEnable(null, null, false);

                  BalanceErrorText.Text = $"Ihr Guthaben reicht nicht aus (min. {FaxCostsInCent}ct).";
                  BalanceErrorText.Visibility = Visibility.Visible;
               }
            }
            catch (NoRightsToFetchBalanceException nex)
            {
               FaxDruckerUtils.LogWarning($"Did not fetch balance due to missing access rights: {nex.Message}");
            }
            catch (Exception ex)
            {
               FaxDruckerUtils.LogCritical($"Error while fetching balance: {ex.Message} {ex.StackTrace}");
            }

         }
      }

      private void BtnSend_KeyDown(object sender, KeyEventArgs e)
      {
         if (e.Key == Key.Enter)
         {
            HandleSendFax();
         }
      }

      private void Window_KeyDown(object sender, KeyEventArgs e)
      {
         if (e.Key == Key.Enter && IsSendButtonEnabled && FaxView.IsVisible)
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

      private async void LogoutUser(bool keycloakLogout = false, string errorMessage = "")
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

         try
         {
            FaxlinesDropdown.SelectedIndex = -1;
            _faxlinesItem.Clear();
            _contactsCollection.Clear();
            _filteredContactsCollection.Clear();

            _isBalanceOkay = true;
            BalanceErrorText.Visibility = Visibility.Hidden;
         }
         catch (Exception ex)
         {
            FaxDruckerUtils.LogCritical($"Exception during reset: {ex.Message}");
         }

         credentialManager.RemoveCredentials();
         ShowPage(FormPage.Login);

         LoginView.showErrorOnLoginView(errorMessage);
      }

      private void CheckShouldEnable(bool? isFaxlineSelected, bool? isValidPhonenumber, bool? isBalanceOkay)
      {
         bool condition1 = _isFaxlineSelected;
         if (isFaxlineSelected != null)
         {
            FaxDruckerUtils.LogInformation($"Changed if faxline is selected: {_isFaxlineSelected}");
            condition1 = (bool)isFaxlineSelected;
            _isFaxlineSelected = condition1;
         }

         bool condition2 = _isValidPhonenumber;
         if (isValidPhonenumber != null)
         {
            FaxDruckerUtils.LogInformation($"Changed if phone number is valid: {_isValidPhonenumber}");
            condition2 = (bool)isValidPhonenumber;
            _isValidPhonenumber = condition2;
         }

         bool condition3 = _isBalanceOkay;
         if (isBalanceOkay != null)
         {
            FaxDruckerUtils.LogInformation($"Changed if balance is okay: {_isBalanceOkay}");
            condition2 = (bool)isBalanceOkay;
            _isBalanceOkay = condition2;
         }

         IsSendButtonEnabled = condition1 && condition2 && condition3;
      }

      private async void HandleSendFax()
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

            _faxManager.FileName = _filename;
            _faxManager.TargetNumber = GetPhoneNumber();

            if (_selectedFaxlineId == null)
            {
               FaxDruckerUtils.LogWarning("No faxline available");
               return;
            }

            var sessionId = await _faxManager.SendFileWithSipgateApi(_selectedFaxlineId, GetApiClient());

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
         if (_phonenumber == null && SelectedContact != null)
         {
            var numberValidationResult = FaxDruckerUtils.GetNumberValidationResult(SelectedContact.Number);
            if (numberValidationResult.wasSuccessful)
            {
               return numberValidationResult.phonenumber;
            }
         }

         return _phonenumber;
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

      private bool DropDownFilter(object item)
      {
         var contact = item as Contact;
         if (contact == null) return false;

         if (contact.IsNoContactFallback)
         {
            return true;
         }

         // No filter
         if (string.IsNullOrEmpty(SearchText))
         {
            return true;
         }

         var searchTextInLower = SearchText.ToLower();

         if (contact.Name.ToLower().Contains(searchTextInLower))
         {
            return true;
         }

         if (contact.Company.ToLower().Contains(searchTextInLower))
         {
            return true;
         }

         if (!contact.IsGermanFaxLine)
         {
            if (contact.Number.ToLower().Contains(SearchText))
            {
               return true;
            }
         }
         else
         {
            if (contact.InternationalFaxLine.Contains(SearchText))
            {
               return true;
            }

            if (contact.NationalFaxLine.Contains(SearchText))
            {
               return true;
            }
         }

         return false;
      }


      private async void InitContacts(SipgateApi apiClient)
      {
         const int contactLimit = 250;
         try
         {
            ContactsView = (CollectionView)new CollectionViewSource { Source = _contactsCollection }.View;

            var initialContactsResponse = await apiClient.GetContactsAsync(contactLimit, 0);
            ProcessContactsResponse(initialContactsResponse);

            if (!initialContactsResponse.TotalCount.HasValue)
            {

               return;
            }

            var totalCount = initialContactsResponse.TotalCount.Value;

            if (totalCount > MaxContactsSupported)
            {
               HasTooManyContacts = true;

               _contactsCollection.Clear();
               _contactsCollection.Insert(0, new Contact("Kein Kontakt", "", "", true));

               return;
            }

            HasTooManyContacts = false;

            FaxDruckerUtils.LogInformation($"Total count of contacts to be fetched: {totalCount}");

            for (var offset = contactLimit + 1; offset <= totalCount; offset += contactLimit)
            {
               ContactsResponse contactsResponse = await apiClient.GetContactsAsync(contactLimit, offset);
               ProcessContactsResponse(contactsResponse);
            }
         }
         catch (Exception ex)
         {
            FaxDruckerUtils.LogCritical($"Error initializing contacts: {ex.Message}");
            return;
         }

         try
         {
            ContactsView.Filter = DropDownFilter;
         }
         catch (Exception ex)
         {
            Console.WriteLine("error setting comboboxitems: " + ex);
         }
      }

      private void ProcessContactsResponse(ContactsResponse contactsResponse)
      {

         foreach (ContactResponse contactResponse in contactsResponse.Items)
         {
            var contactObjects = MapContactReponseToContactObjects(contactResponse);
            foreach (Contact contact in contactObjects)
            {
               _contactsCollection.Add(contact);
            }
         }
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

         if (_mixpanel != null)
         {
            if (page == FormPage.Login)
            {
               FaxDruckerUtils.LogInformation("Sending PageView Event to Mixpanel");
               _ = _mixpanel.TrackPageView("/LoginView");

            }
            else if (page == FormPage.SendingStatus)
            {
               FaxDruckerUtils.LogInformation("Sending PageView Event to Mixpanel");
               _ = _mixpanel.TrackPageView("/StatusView");

            }
         }

      }

      public async Task SetEligibleFaxlines(SipgateApi apiClient)
      {
         if (FaxlinesDropdown.Items.Count > 0)
         {
            FaxDruckerUtils.LogInformation("Combobox items exist already");
            return;
         }

         try
         {
            FaxlinesResponse faxlines = null;
            GroupFaxlinesResponse groupFaxlines;
            FaxlineErrorText.Visibility = Visibility.Collapsed;

            _faxManager = new FaxManager();
            try
            {
               faxlines = await _faxManager.GetFaxLineAsync(apiClient);
            }
            catch (Exception e)
            {
               FaxDruckerUtils.LogCritical($"Error fetching faxlines: {e.Message}");
            }

            if (faxlines == null || faxlines.Items == null || faxlines.Items.Count == 0)
            {
               FaxDruckerUtils.LogWarning("No faxlines exist");
               FaxlineErrorText.Text = "Sie haben aktuell keinen Faxanschluss.";
               FaxlineErrorText.Visibility = Visibility.Visible;
               return;
            }

            foreach (FaxlineResponse faxline in faxlines.Items)
            {
               if (faxline.CanSend.HasValue && faxline.CanSend.Value)
               {
                  _faxlinesItem.Add(new SelectableFaxline(faxline));
               }
            }

            groupFaxlines = await _faxManager.GetGroupFaxLineAsync(apiClient);

            foreach (GroupFaxlineResponse groupFaxline in groupFaxlines.Items)
            {
               if (groupFaxline.CanSend.HasValue && groupFaxline.CanSend.Value)
               {
                  _faxlinesItem.Add(new SelectableFaxline(groupFaxline));
               }
            }

            if (_faxlinesItem.Count > 0)
            {
               FaxDruckerUtils.LogCritical("faxlinesItem Count:" + _faxlinesItem.Count);
               FaxlinesDropdown.SelectedIndex = 0;
            }

         }
         catch (Exception ex)
         {
            FaxDruckerUtils.LogCritical($"Es ist ein Fehler aufgetreten: {ex.Message} - {ex.StackTrace}");
         }
      }
      #endregion

      public void Dispose()
      {
         // Suppress finalization.
         GC.SuppressFinalize(this);
      }

      private void TargetNumberComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
      {
         Contact selectedContact = e.AddedItems.Count > 0 ? (Contact)e.AddedItems[0] : null;

         if (selectedContact != null && selectedContact.IsValidFaxNumber())
         {
            CheckShouldEnable(null, true, null);
            return;
         }

         CheckShouldEnable(null, false, null);
      }


      private void TargetNumberComboBox_OnTextChanged(object sender, TextChangedEventArgs e)
      {
         CustomCombobox cb = (CustomCombobox)sender;
         bool isNoContact = false;
         string comboBoxText = cb.Text;


         if (cb.Items.Cast<Object>().AsParallel().Any(contact =>
         {
            Contact current = (Contact)contact;

            if (!current.IsGermanFaxLine)
            {
               return current.Number.Replace("+", "") == comboBoxText.Replace("+", "");
            }
            else
            {
               if (current.NationalFaxLine.Contains(comboBoxText))
               {
                  return true;
               }

               if (current.InternationalFaxLine.Contains(comboBoxText))
               {
                  return true;
               }
            }

            return false;
         }) || comboBoxText == "")
         {
            _contactsCollection.Remove(_contactsCollection.FirstOrDefault(contact => contact.IsNoContactFallback));
         }
         else
         {
            if (!_contactsCollection.Any(contact => contact.IsNoContactFallback))
            {
               _contactsCollection.Insert(0, new Contact("Kein Kontakt", cb.Text, "", true));
            }

            foreach (var contact in _contactsCollection)
            {
               if (contact.IsNoContactFallback)
               {
                  contact.ChangeNumber(cb.Text);
               }
            }

            isNoContact = true;
         }

         cb.MaxDropDownHeight = 300;

         try
         {
            var content = cb.Text;

            NumberValidationResult validationResult = FaxDruckerUtils.GetNumberValidationResult(content);


            if (string.IsNullOrEmpty(content))
            {
               CheckShouldEnable(null, false, null);

               ValidationMessageText.Text = FaxDruckerUtils.numberValidationMessages[3];
               ValidationMessageText.Visibility = Visibility.Visible;
               ValidationMessageText.Foreground =
                   new SolidColorBrush((Color)ColorConverter.ConvertFromString("#c90c2f"));

               return;
            }


            string validationMessage = FaxDruckerUtils.numberValidationMessages[(int)validationResult.validationMessageCode];

            if (ValidationMessageText == null)
            {
               return;
            }

            if (validationResult.wasSuccessful)
            {
               CheckShouldEnable(null, true, null);

               _phonenumber = validationResult.phonenumber;
               if (isNoContact && !_hasTooManyContacts)
               {
                  ValidationMessageText.Text = "Nummer ist kein Kontakt";
                  ValidationMessageText.Visibility = Visibility.Visible;
                  ValidationMessageText.Foreground =
                      new SolidColorBrush((Color)ColorConverter.ConvertFromString("#838383"));
               }
               else
               {
                  ValidationMessageText.Visibility = Visibility.Collapsed;
               }

               FaxDruckerUtils.LogInformation($"Validation of {_phonenumber}: {validationMessage}");
            }
            else
            {
               CheckShouldEnable(null, false, null);

               ValidationMessageText.Text = validationMessage;
               ValidationMessageText.Foreground =
                   new SolidColorBrush((Color)ColorConverter.ConvertFromString("#c90c2f"));
               ValidationMessageText.Visibility = Visibility.Visible;

               if (validationResult.exception != null)
               {
                  FaxDruckerUtils.LogCritical($"Encountered error while attempting to validate phone number: {validationResult.exception.Message}");
               }
            }
         }
         catch (Exception err)
         {
            FaxDruckerUtils.LogCritical($"Encountered error during target number change: {err.Message}");
         }
      }

      private void TargetNumberComboBox_OnGotFocus(object sender, RoutedEventArgs e)
      {
         TargetNumberComboBox.IsDropDownOpen = true;
      }
   }
}
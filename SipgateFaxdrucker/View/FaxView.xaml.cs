using SipgateFaxdrucker.SipgateAPI;
using SipgateFaxdrucker.SipgateAPI.Models;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using static SipgateFaxdrucker.SipgateAPI.SipgateApi;

namespace SipgateFaxdrucker.View
{
   /// <summary>
   /// Interaction logic for FaxView.xaml
   /// </summary>
   public partial class FaxView : UserControl, INotifyPropertyChanged
   {
      readonly ObservableCollection<Contact> _contactsCollection = new ObservableCollection<Contact>();
      readonly ObservableCollection<Contact> _filteredContactsCollection = new ObservableCollection<Contact>();

      ObservableCollection<SelectableFaxline> _faxlinesItem = new ObservableCollection<SelectableFaxline>();
      public ObservableCollection<SelectableFaxline> Faxlines => _faxlinesItem;

      private const double FaxCostsInCent = 1.79;
      private const int MaxContactsSupported = 1000;
      private UserinfoResponse _userinfo;

      private bool _isFaxlineSelected;
      private bool _isValidPhonenumber;
      private bool _isBalanceOkay = true;
      private bool _isSendButtonEnabled;

      public string selectedFaxlineId {
         get; private set;
      }

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

      private void onLogOut(bool keycloakLogout = false, string errorMessage = "")
      {
         _isBalanceOkay = true;
         _contactsCollection.Clear();
         _filteredContactsCollection.Clear();


         try
         {
            FaxlinesDropdown.SelectedIndex = -1;
            _faxlinesItem.Clear();

            BalanceErrorText.Visibility = Visibility.Hidden;
         }
         catch (Exception ex)
         {
            FaxDruckerUtils.LogCritical($"Exception during reset: {ex.Message}");
         }

         SipgateForm.Instance.LogoutUser(keycloakLogout, errorMessage);
      }

      public event PropertyChangedEventHandler PropertyChanged;

      private void OnPropertyChanged([CallerMemberName] string propertyName = null)
      {
         PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
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

      public FaxView()
      {
         InitializeComponent();
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

      private void TargetNumberComboBox_OnGotFocus(object sender, RoutedEventArgs e)
      {
         TargetNumberComboBox.IsDropDownOpen = true;
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

      public void CheckShouldEnable(bool? isFaxlineSelected, bool? isValidPhonenumber, bool? isBalanceOkay)
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

      private async void FaxView_IsVisibleChanged(object sender, DependencyPropertyChangedEventArgs e)
      {
         var faxView = (FrameworkElement)sender;

         if (faxView.Visibility == Visibility.Visible)
         {
            SipgateApi apiClient = SipgateForm.Instance.GetApiClient();

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
                  onLogOut(false, "Es gab eine fehlerhafte Antwort vom Server.");
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
               onLogOut(false, "Beim Laden der Kontakte ist ein Fehler aufgetreten.");
               return;
            }

            try
            {
               FaxDruckerUtils.LogInformation("Initializing Mixpanel");

               SipgateForm.mixpanel = new Mixpanel(_userinfo);

               var success = await SipgateForm.mixpanel.TrackPageView("/FaxView");
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
               await SipgateForm.Instance.SetEligibleFaxlines(SipgateForm.Instance.GetApiClient());
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

               SipgateForm.Instance.phonenumber = validationResult.phonenumber;
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

               FaxDruckerUtils.LogInformation($"Validation of {SipgateForm.Instance.phonenumber}: {validationMessage}");
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

            SipgateForm.Instance.faxManager = new FaxManager();
            try
            {
               faxlines = await SipgateForm.Instance.faxManager.GetFaxLineAsync(apiClient);
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

            groupFaxlines = await SipgateForm.Instance.faxManager.GetGroupFaxLineAsync(apiClient);

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
            selectedFaxlineId = faxline.Id;
            CheckShouldEnable(true, null, null);

            FaxDruckerUtils.LogInformation($"Selected Faxline: {faxline.Id}");
         }
         else
         {
            FaxDruckerUtils.LogError("Error casting selected Item");
         }

      }

      private void LogoutButton_Click(object sender, RoutedEventArgs e)
      {
         onLogOut(true);
      }

      private void BtnSend_Click(object sender, RoutedEventArgs e)
      {
         SipgateForm.Instance.HandleSendFax();
      }

      private void BtnSend_KeyDown(object sender, KeyEventArgs e)
      {
         if (e.Key == Key.Enter)
         {
           SipgateForm.Instance.HandleSendFax();
         }
      }  
   }
}

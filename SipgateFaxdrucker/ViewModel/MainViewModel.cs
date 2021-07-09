using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Data;

namespace SipgateFaxdrucker
{
    public class MainViewModel : INotifyPropertyChanged
    {
        public MainViewModel()
        {
            Contacts = (CollectionView)new CollectionViewSource { Source = new ObservableCollection<Contact>() }.View;
            Contacts.Filter = DropDownFilter;
        }

        #region ComboBox

        public CollectionView Contacts { get; }

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

        private bool _comboOpen;
        public bool ComboOpen
        {
            get => _comboOpen;
            set
            {
                _comboOpen = value;
                OnPropertyChanged();
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;

        private string _searchText;
        public string SearchText
        {
            get => _searchText;
            set
            {
                if (value != null)
                {
                    _searchText = value;
                    OnPropertyChanged();
                    if (SelectedContact == null || _searchText != SelectedContact.Number)
                    {
                        Contacts.Refresh();
                    }
                }
            }
        }

        private bool DropDownFilter(object item)
        {
            var contact = item as Contact;
            if (contact == null) return false;

            // No filter
            if (string.IsNullOrEmpty(SearchText)) return true;
            //         
            if (contact.Name.ToLower().Contains(SearchText.ToLower()))
            {
                return true;
            }

            if (contact.Company.ToLower().Contains(SearchText.ToLower()))
            {
                return true;
            }

            if (contact.Number.ToLower().Contains(SearchText.ToLower()))
            {
                return true;
            }

            return false;
        }

        #endregion ComboBox

        private void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
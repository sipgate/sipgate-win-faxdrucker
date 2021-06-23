using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace SipgateFaxdrucker
{
    public class Contact : INotifyPropertyChanged
    {
        public Contact(string name, string number, string company, bool isNoContactFallback = false)
        {
            Name = name;
            Number = number;
            Company = company;
            IsNoContactFallback = isNoContactFallback;
            GenerateFaxlineVariation(number);
        }

        private void GenerateFaxlineVariation(string number)
        {
            if (number.StartsWith("0"))
            {
                NationalFaxLine = number;
                InternationalFaxLine = number.Remove(0, 1).Insert(0, "+49");
                IsGermanFaxLine = true;
                return;
            }

            if (number.StartsWith("+49"))
            {
                InternationalFaxLine = number;
                NationalFaxLine = number.Remove(0, 3).Insert(0, "0");
                IsGermanFaxLine = true;
                return;
            }

            IsGermanFaxLine = false;
            Number = number;
        }

        public bool IsNoContactFallback { get; }
        public string Name { get; }
        public string Number { get; private set; }
        public string Company { get; }
        public string NationalFaxLine { get; private set; }
        public string InternationalFaxLine { get; private set; }
        public bool IsGermanFaxLine { get; private set; }

        public string DisplayCompany => string.IsNullOrEmpty(Company) ? "" : $" ({Company})";

        public override string ToString()
        {
            return $"{Name} {Company} {Number}";
        }

        public void ChangeNumber(string number)
        {
            Number = number;
            OnPropertyChanged(nameof(Number));
        }

        public bool IsValidFaxNumber()
        {
            return  !IsNoContactFallback && Utils.GetNumberValidationResult(Number).wasSuccessful;
        }

        public event PropertyChangedEventHandler PropertyChanged;

        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
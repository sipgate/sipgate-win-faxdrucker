using System.Windows.Media;

namespace SipgateFaxdrucker
{
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
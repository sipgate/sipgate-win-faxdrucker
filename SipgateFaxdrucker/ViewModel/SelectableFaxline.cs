using SipgateFaxdrucker.SipgateAPI.Models;

namespace SipgateFaxdrucker
{
    public class SelectableFaxline
    {
        public string Id { get; }
        public string Alias { get; }

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
    }
}
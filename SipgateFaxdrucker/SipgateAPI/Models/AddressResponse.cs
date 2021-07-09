using Newtonsoft.Json;
using System.Runtime.Serialization;
using System.Text;

namespace SipgateFaxdrucker.SipgateAPI.Models
{

    /// <summary>
    /// 
    /// </summary>
    [DataContract]
    public class AddressResponse
    {
        /// <summary>
        /// Gets or Sets PoBox
        /// </summary>
        [DataMember(Name = "poBox", EmitDefaultValue = false)]
        [JsonProperty(PropertyName = "poBox")]
        public string PoBox { get; set; }

        /// <summary>
        /// Gets or Sets ExtendedAddress
        /// </summary>
        [DataMember(Name = "extendedAddress", EmitDefaultValue = false)]
        [JsonProperty(PropertyName = "extendedAddress")]
        public string ExtendedAddress { get; set; }

        /// <summary>
        /// Gets or Sets StreetAddress
        /// </summary>
        [DataMember(Name = "streetAddress", EmitDefaultValue = false)]
        [JsonProperty(PropertyName = "streetAddress")]
        public string StreetAddress { get; set; }

        /// <summary>
        /// Gets or Sets Locality
        /// </summary>
        [DataMember(Name = "locality", EmitDefaultValue = false)]
        [JsonProperty(PropertyName = "locality")]
        public string Locality { get; set; }

        /// <summary>
        /// Gets or Sets Region
        /// </summary>
        [DataMember(Name = "region", EmitDefaultValue = false)]
        [JsonProperty(PropertyName = "region")]
        public string Region { get; set; }

        /// <summary>
        /// Gets or Sets PostalCode
        /// </summary>
        [DataMember(Name = "postalCode", EmitDefaultValue = false)]
        [JsonProperty(PropertyName = "postalCode")]
        public string PostalCode { get; set; }

        /// <summary>
        /// Gets or Sets Country
        /// </summary>
        [DataMember(Name = "country", EmitDefaultValue = false)]
        [JsonProperty(PropertyName = "country")]
        public string Country { get; set; }


        /// <summary>
        /// Get the string presentation of the object
        /// </summary>
        /// <returns>String presentation of the object</returns>
        public override string ToString()
        {
            var sb = new StringBuilder();
            sb.Append("class AddressResponse {\n");
            sb.Append("  PoBox: ").Append(PoBox).Append("\n");
            sb.Append("  ExtendedAddress: ").Append(ExtendedAddress).Append("\n");
            sb.Append("  StreetAddress: ").Append(StreetAddress).Append("\n");
            sb.Append("  Locality: ").Append(Locality).Append("\n");
            sb.Append("  Region: ").Append(Region).Append("\n");
            sb.Append("  PostalCode: ").Append(PostalCode).Append("\n");
            sb.Append("  Country: ").Append(Country).Append("\n");
            sb.Append("}\n");
            return sb.ToString();
        }

        /// <summary>
        /// Get the JSON string presentation of the object
        /// </summary>
        /// <returns>JSON string presentation of the object</returns>
        public string ToJson()
        {
            return JsonConvert.SerializeObject(this, Formatting.Indented);
        }

    }
}

using Newtonsoft.Json;
using System.Collections.Generic;
using System.Runtime.Serialization;
using System.Text;

namespace SipgateFaxdrucker.SipgateAPI.Models
{

    /// <summary>
    /// 
    /// </summary>
    [DataContract]
    public class InternalContactResponse
    {
        /// <summary>
        /// Gets or Sets Id
        /// </summary>
        [DataMember(Name = "id", EmitDefaultValue = false)]
        [JsonProperty(PropertyName = "id")]
        public string Id { get; set; }

        /// <summary>
        /// Gets or Sets Name
        /// </summary>
        [DataMember(Name = "name", EmitDefaultValue = false)]
        [JsonProperty(PropertyName = "name")]
        public string Name { get; set; }

        /// <summary>
        /// Gets or Sets Emails
        /// </summary>
        [DataMember(Name = "emails", EmitDefaultValue = false)]
        [JsonProperty(PropertyName = "emails")]
        public List<string> Emails { get; set; }

        /// <summary>
        /// Gets or Sets Mobile
        /// </summary>
        [DataMember(Name = "mobile", EmitDefaultValue = false)]
        [JsonProperty(PropertyName = "mobile")]
        public List<string> Mobile { get; set; }

        /// <summary>
        /// Gets or Sets Landline
        /// </summary>
        [DataMember(Name = "landline", EmitDefaultValue = false)]
        [JsonProperty(PropertyName = "landline")]
        public List<string> Landline { get; set; }

        /// <summary>
        /// Gets or Sets Fax
        /// </summary>
        [DataMember(Name = "fax", EmitDefaultValue = false)]
        [JsonProperty(PropertyName = "fax")]
        public List<string> Fax { get; set; }

        /// <summary>
        /// Gets or Sets Directdial
        /// </summary>
        [DataMember(Name = "directdial", EmitDefaultValue = false)]
        [JsonProperty(PropertyName = "directdial")]
        public List<string> Directdial { get; set; }


        /// <summary>
        /// Get the string presentation of the object
        /// </summary>
        /// <returns>String presentation of the object</returns>
        public override string ToString()
        {
            var sb = new StringBuilder();
            sb.Append("class InternalContactResponse {\n");
            sb.Append("  Id: ").Append(Id).Append("\n");
            sb.Append("  Name: ").Append(Name).Append("\n");
            sb.Append("  Emails: ").Append(Emails).Append("\n");
            sb.Append("  Mobile: ").Append(Mobile).Append("\n");
            sb.Append("  Landline: ").Append(Landline).Append("\n");
            sb.Append("  Fax: ").Append(Fax).Append("\n");
            sb.Append("  Directdial: ").Append(Directdial).Append("\n");
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

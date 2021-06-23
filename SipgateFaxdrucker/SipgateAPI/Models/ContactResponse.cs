using System.Collections.Generic;
using System.Runtime.Serialization;
using System.Text;
using Newtonsoft.Json;

namespace SipgateFaxdrucker.SipgateAPI.Models {

  /// <summary>
  /// 
  /// </summary>
  [DataContract]
  public class ContactResponse {
    /// <summary>
    /// Gets or Sets Id
    /// </summary>
    [DataMember(Name="id", EmitDefaultValue=false)]
    [JsonProperty(PropertyName = "id")]
    public string Id { get; set; }

    /// <summary>
    /// Gets or Sets Name
    /// </summary>
    [DataMember(Name="name", EmitDefaultValue=false)]
    [JsonProperty(PropertyName = "name")]
    public string Name { get; set; }

    /// <summary>
    /// Gets or Sets Picture
    /// </summary>
    [DataMember(Name="picture", EmitDefaultValue=false)]
    [JsonProperty(PropertyName = "picture")]
    public string Picture { get; set; }

    /// <summary>
    /// Gets or Sets Emails
    /// </summary>
    [DataMember(Name="emails", EmitDefaultValue=false)]
    [JsonProperty(PropertyName = "emails")]
    public List<EMailResponse> Emails { get; set; }

    /// <summary>
    /// Gets or Sets Numbers
    /// </summary>
    [DataMember(Name="numbers", EmitDefaultValue=false)]
    [JsonProperty(PropertyName = "numbers")]
    public List<NumberResponse> Numbers { get; set; }

    /// <summary>
    /// Gets or Sets Addresses
    /// </summary>
    [DataMember(Name="addresses", EmitDefaultValue=false)]
    [JsonProperty(PropertyName = "addresses")]
    public List<AddressResponse> Addresses { get; set; }

    /// <summary>
    /// Gets or Sets Organization
    /// </summary>
    [DataMember(Name="organization", EmitDefaultValue=false)]
    [JsonProperty(PropertyName = "organization")]
    public List<List<string>> Organization { get; set; }

    /// <summary>
    /// Gets or Sets Scope
    /// </summary>
    [DataMember(Name="scope", EmitDefaultValue=false)]
    [JsonProperty(PropertyName = "scope")]
    public string Scope { get; set; }


    /// <summary>
    /// Get the string presentation of the object
    /// </summary>
    /// <returns>String presentation of the object</returns>
    public override string ToString()  {
      var sb = new StringBuilder();
      sb.Append("class ContactResponse {\n");
      sb.Append("  Id: ").Append(Id).Append("\n");
      sb.Append("  Name: ").Append(Name).Append("\n");
      sb.Append("  Picture: ").Append(Picture).Append("\n");
      sb.Append("  Emails: ").Append(Emails).Append("\n");
      sb.Append("  Numbers: ").Append(Numbers).Append("\n");
      sb.Append("  Addresses: ").Append(Addresses).Append("\n");
      sb.Append("  Organization: ").Append(Organization).Append("\n");
      sb.Append("  Scope: ").Append(Scope).Append("\n");
      sb.Append("}\n");
      return sb.ToString();
    }

    /// <summary>
    /// Get the JSON string presentation of the object
    /// </summary>
    /// <returns>JSON string presentation of the object</returns>
    public string ToJson() {
      return JsonConvert.SerializeObject(this, Formatting.Indented);
    }

}
}

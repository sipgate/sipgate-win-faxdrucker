﻿// Code generated by Microsoft (R) AutoRest Code Generator 0.16.0.0
// Changes may cause incorrect behavior and will be lost if the code is
// regenerated.

using Newtonsoft.Json;

namespace SipgateFaxdrucker.SipgateAPI.Models
{
    public partial class UserinfoResponse
    {
        /// <summary>
        /// Initializes a new instance of the UserinfoResponse class.
        /// </summary>
        public UserinfoResponse() { }

        /// <summary>
        /// Initializes a new instance of the UserinfoResponse class.
        /// </summary>
        public UserinfoResponse(string sub = default, string domain = default, string masterSipId = default, string locale = default, bool isTestAccount = false,
        bool isAdmin = false,
        string product = "",
        string[] flags = default)
        {
            Sub = sub;
            Domain = domain;
            MasterSipId = masterSipId;
            Locale = locale;
            IsTestAccount = isTestAccount;
            IsAdmin = isAdmin;
            Product = product;
            Flags = flags;
        }

        /// <summary>
        /// </summary>
        [JsonProperty(PropertyName = "sub")]
        public string Sub { get; set; }

        /// <summary>
        /// </summary>
        [JsonProperty(PropertyName = "domain")]
        public string Domain { get; set; }

        /// <summary>
        /// </summary>
        [JsonProperty(PropertyName = "masterSipId")]
        public string MasterSipId { get; set; }

        /// <summary>
        /// </summary>
        [JsonProperty(PropertyName = "locale")]
        public string Locale { get; set; }

        [JsonProperty(PropertyName = "isTestAccount")]
        public bool IsTestAccount { get; set; }

        [JsonProperty(PropertyName = "isAdmin")]
        public bool IsAdmin { get; set; }

        [JsonProperty(PropertyName = "product")]
        public string Product { get; set; }

        [JsonProperty(PropertyName = "flags")]
        public string[] Flags { get; set; }


    }
}

﻿// Code generated by Microsoft (R) AutoRest Code Generator 0.16.0.0
// Changes may cause incorrect behavior and will be lost if the code is
// regenerated.

using Newtonsoft.Json;
using System.Collections.Generic;

namespace SipgateFaxdrucker.SipgateAPI.Models
{
    public partial class FaxlinesResponse
    {
        /// <summary>
        /// Initializes a new instance of the FaxlinesResponse class.
        /// </summary>
        public FaxlinesResponse() { }

        /// <summary>
        /// Initializes a new instance of the FaxlinesResponse class.
        /// </summary>
        public FaxlinesResponse(IList<FaxlineResponse> items = default)
        {
            Items = items;
        }

        /// <summary>
        /// </summary>
        [JsonProperty(PropertyName = "items")]
        public IList<FaxlineResponse> Items { get; set; }

    }
}

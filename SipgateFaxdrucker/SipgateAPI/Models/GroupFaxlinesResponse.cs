﻿// Code generated by Microsoft (R) AutoRest Code Generator 0.16.0.0
// Changes may cause incorrect behavior and will be lost if the code is
// regenerated.

using Newtonsoft.Json;
using System.Collections.Generic;

namespace SipgateFaxdrucker.SipgateAPI.Models
{
    public partial class GroupFaxlinesResponse
    {
        /// <summary>
        /// Initializes a new instance of the GroupFaxlinesResponse class.
        /// </summary>
        public GroupFaxlinesResponse() { }

        /// <summary>
        /// Initializes a new instance of the GroupFaxlinesResponse class.
        /// </summary>
        public GroupFaxlinesResponse(IList<GroupFaxlineResponse> items = default)
        {
            Items = items;
        }

        /// <summary>
        /// </summary>
        [JsonProperty(PropertyName = "items")]
        public IList<GroupFaxlineResponse> Items { get; set; }

    }
}

﻿// Code generated by Microsoft (R) AutoRest Code Generator 0.16.0.0
// Changes may cause incorrect behavior and will be lost if the code is
// regenerated.

using System.Collections.Generic;
using System.Linq;
using Microsoft.Rest;
using Newtonsoft.Json;

namespace SipgateFaxdrucker.SipgateAPI.Models
{
    public partial class HistoryEntryResponse
    {
        /// <summary>
        /// Initializes a new instance of the HistoryEntryResponse class.
        /// </summary>
        public HistoryEntryResponse() { }

        /// <summary>
        /// Initializes a new instance of the HistoryEntryResponse class.
        /// </summary>
        public HistoryEntryResponse(string id = default, string source = default, string target = default, string sourceAlias = default, string targetAlias = default, string type = default, string created = default, string lastModified = default, string direction = default, bool? incoming = default, string status = default, IList<string> connectionIds = default, bool? read = default, bool? archived = default, string note = default, IList<RoutedEndpointResponse> endpoints = default, bool? starred = default, IList<string> labels = default, string faxStatusType = default)
        {
            Id = id;
            Source = source;
            Target = target;
            SourceAlias = sourceAlias;
            TargetAlias = targetAlias;
            Type = type;
            Created = created;
            LastModified = lastModified;
            Direction = direction;
            Incoming = incoming;
            Status = status;
            ConnectionIds = connectionIds;
            Read = read;
            Archived = archived;
            Note = note;
            Endpoints = endpoints;
            Starred = starred;
            Labels = labels;
            FaxStatusType = faxStatusType;
        }

        /// <summary>
        /// </summary>
        [JsonProperty(PropertyName = "id")]
        public string Id { get; set; }

        /// <summary>
        /// </summary>
        [JsonProperty(PropertyName = "source")]
        public string Source { get; set; }

        /// <summary>
        /// </summary>
        [JsonProperty(PropertyName = "target")]
        public string Target { get; set; }

        /// <summary>
        /// </summary>
        [JsonProperty(PropertyName = "sourceAlias")]
        public string SourceAlias { get; set; }

        /// <summary>
        /// </summary>
        [JsonProperty(PropertyName = "targetAlias")]
        public string TargetAlias { get; set; }

        /// <summary>
        /// Possible values include: 'CALL', 'VOICEMAIL', 'SMS', 'FAX'
        /// </summary>
        [JsonProperty(PropertyName = "type")]
        public string Type { get; set; }

        /// <summary>
        /// </summary>
        [JsonProperty(PropertyName = "created")]
        public string Created { get; set; }

        /// <summary>
        /// </summary>
        [JsonProperty(PropertyName = "lastModified")]
        public string LastModified { get; set; }

        /// <summary>
        /// Possible values include: 'INCOMING', 'OUTGOING',
        /// 'MISSED_INCOMING', 'MISSED_OUTGOING'
        /// </summary>
        [JsonProperty(PropertyName = "direction")]
        public string Direction { get; set; }

        /// <summary>
        /// </summary>
        [JsonProperty(PropertyName = "incoming")]
        public bool? Incoming { get; set; }

        /// <summary>
        /// Possible values include: 'NOPICKUP', 'BUSY', 'PICKUP', 'FORWARD'
        /// </summary>
        [JsonProperty(PropertyName = "status")]
        public string Status { get; set; }

        /// <summary>
        /// </summary>
        [JsonProperty(PropertyName = "connectionIds")]
        public IList<string> ConnectionIds { get; set; }

        /// <summary>
        /// </summary>
        [JsonProperty(PropertyName = "read")]
        public bool? Read { get; set; }

        /// <summary>
        /// </summary>
        [JsonProperty(PropertyName = "archived")]
        public bool? Archived { get; set; }

        /// <summary>
        /// </summary>
        [JsonProperty(PropertyName = "note")]
        public string Note { get; set; }

        /// <summary>
        /// </summary>
        [JsonProperty(PropertyName = "endpoints")]
        public IList<RoutedEndpointResponse> Endpoints { get; set; }

        /// <summary>
        /// </summary>
        [JsonProperty(PropertyName = "starred")]
        public bool? Starred { get; set; }

        /// <summary>
        /// </summary>
        [JsonProperty(PropertyName = "labels")]
        public IList<string> Labels { get; set; }

        /// <summary>
        /// Possible values include: 'PENDING', 'SENDING', 'FAILED', 'SENT'
        /// </summary>
        [JsonProperty(PropertyName = "faxStatusType")]
        public string FaxStatusType { get; set; }

        /// <summary>
        /// Validate the object. Throws ValidationException if validation fails.
        /// </summary>
        public virtual void Validate()
        {
            if (this.Labels != null)
            {
                if (this.Labels.Count != this.Labels.Distinct().Count())
                {
                    throw new ValidationException(ValidationRules.UniqueItems, "Labels");
                }
            }
        }
    }
}
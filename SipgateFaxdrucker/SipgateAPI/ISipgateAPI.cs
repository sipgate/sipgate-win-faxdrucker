﻿// Code generated by Microsoft (R) AutoRest Code Generator 0.16.0.0
// Changes may cause incorrect behavior and will be lost if the code is
// regenerated.

using Microsoft.Rest;
using Newtonsoft.Json;
using SipgateFaxdrucker.SipgateAPI.Models;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace SipgateFaxdrucker.SipgateAPI
{
    /// <summary>
    /// This is the sipgate REST API documentation. We build our applications
    /// on this API and we invite you to use it too.
    /// </summary>
    public partial interface ISipgateApi : IDisposable
    {
        /// <summary>
        /// The base URI of the service.
        /// </summary>
        Uri BaseUri { get; set; }

        /// <summary>
        /// Gets or sets json serialization settings.
        /// </summary>
        JsonSerializerSettings SerializationSettings { get; }

        /// <summary>
        /// Gets or sets json deserialization settings.
        /// </summary>
        JsonSerializerSettings DeserializationSettings { get; }

        /// <summary>
        /// Subscription credentials which uniquely identify client
        /// subscription.
        /// </summary>
        ServiceClientCredentials Credentials { get; }


        /// <summary>
        /// Send a fax
        /// </summary>
        /// <param name='body'>
        /// </param>
        /// <param name='customHeaders'>
        /// The headers that will be added to request.
        /// </param>
        /// <param name='cancellationToken'>
        /// The cancellation token.
        /// </param>
        Task<HttpOperationResponse<SendFaxSessionResponse>> SendFaxWithHttpMessagesAsync(SendFaxRequest body = default, Dictionary<string, List<string>> customHeaders = null, CancellationToken cancellationToken = default);

        /// <summary>
        /// Get information about the logged in user
        /// </summary>
        /// <param name='authorization'>
        /// token
        /// </param>
        /// <param name='customHeaders'>
        /// The headers that will be added to request.
        /// </param>
        /// <param name='cancellationToken'>
        /// The cancellation token.
        /// </param>
        Task<HttpOperationResponse<UserinfoResponse>> UserinfoWithHttpMessagesAsync(string authorization = default, Dictionary<string, List<string>> customHeaders = null, CancellationToken cancellationToken = default);

        /// <summary>
        /// List all fax lines
        /// </summary>
        /// <param name='userId'>
        /// The unique user identifier
        /// </param>
        /// <param name='customHeaders'>
        /// The headers that will be added to request.
        /// </param>
        /// <param name='cancellationToken'>
        /// The cancellation token.
        /// </param>
        Task<HttpOperationResponse<FaxlinesResponse>> GetUserFaxlinesWithHttpMessagesAsync(string userId, Dictionary<string, List<string>> customHeaders = null, CancellationToken cancellationToken = default);

        /// <summary>
        /// Get a specific call, fax, SMS or voicemail
        /// </summary>
        /// <param name='entryId'>
        /// The unique call, fax, sms or voicemail identifier
        /// </param>
        /// <param name='customHeaders'>
        /// The headers that will be added to request.
        /// </param>
        /// <param name='cancellationToken'>
        /// The cancellation token.
        /// </param>
        Task<HttpOperationResponse<HistoryEntryResponse>> GetHistoryByIdWithHttpMessagesAsync(string entryId, Dictionary<string, List<string>> customHeaders = null, CancellationToken cancellationToken = default);

        /// <summary>
        /// List all group fax lines
        /// </summary>
        /// <param name='userId'>
        /// The unique user identifier
        /// </param>
        /// <param name='customHeaders'>
        /// The headers that will be added to request.
        /// </param>
        /// <param name='cancellationToken'>
        /// The cancellation token.
        /// </param>
        Task<HttpOperationResponse<GroupFaxlinesResponse>> GetGroupFaxlinesForUserWithHttpMessagesAsync(string userId = default, Dictionary<string, List<string>> customHeaders = null, CancellationToken cancellationToken = default);

        /// <summary>
        /// List all fax lines
        /// </summary>
        /// <param name="offset"></param>
        /// <param name='customHeaders'>
        /// The headers that will be added to request.
        /// </param>
        /// <param name='cancellationToken'>
        /// The cancellation token.
        /// </param>
        /// <param name="limit"></param>
        Task<HttpOperationResponse<ContactsResponse>> GetContactsWithHttpMessagesAsync(int? limit, int? offset, Dictionary<string, List<string>> customHeaders = null, CancellationToken cancellationToken = default);

        /// <summary>
        /// Get balance of account
        /// </summary>
        /// <param name="customHeaders"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        Task<HttpOperationResponse<BalanceResponse>> BalanceWithHttpMessagesAsync(Dictionary<string, List<string>> customHeaders = null, CancellationToken cancellationToken = default);
    }
}

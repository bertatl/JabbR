using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text.Json;
using Microsoft.AspNetCore.Http;
using JabbR.WebApi.Model;

namespace JabbR.Infrastructure
{
    public static class HttpRequestExtensions
    {
        private static IHttpContextAccessor _httpContextAccessor;

        public static void Configure(IHttpContextAccessor httpContextAccessor)
        {
            _httpContextAccessor = httpContextAccessor;
        }
        /// <summary>
        /// Returns a success message for the given data. This is returned to the client using the supplied status code
        /// </summary>
        /// <typeparam name="T">Type of the payload (usually inferred).</typeparam>
        /// <param name="request">Request message that is used to create output message.</param>
        /// <param name="statusCode">Status code to return to the client</param>
        /// <param name="data">API payload</param>
        /// <param name="filenamePrefix">Filename to return to the client, if client requests so.</param>
        /// <returns>
        /// HttpResponseMessage that wraps the given payload
        /// </returns>
        public static HttpResponseMessage CreateJabbrSuccessMessage<T>(this HttpRequestMessage request, HttpStatusCode statusCode, T data, string filenamePrefix)
        {
            var responseMessage = new HttpResponseMessage(statusCode)
            {
                Content = new ObjectContent<T>(data, new System.Net.Http.Formatting.JsonMediaTypeFormatter())
            };
            return AddResponseHeaders(request, responseMessage, filenamePrefix);
        }
        /// <summary>
        /// Returns a success message for the given data. This is returned to the client using the supplied status code
        /// </summary>
        /// <typeparam name="T">Type of the payload (usually inferred).</typeparam>
        /// <param name="data">API payoad</param>
        /// <param name="request">Request message that is used to create output message.</param>
        /// <param name="statusCode">Status code to return to the client</param>
        /// <param name="filenamePrefix">Filename to return to the client, if client requests so.</param>
        /// <returns>HttpResponseMessage that wraps the given payload</returns>
        public static HttpResponseMessage CreateJabbrSuccessMessage<T>(this HttpRequestMessage Request, HttpStatusCode statusCode, T data)
        {
            var responseMessage = new HttpResponseMessage(statusCode)
            {
                Content = new ObjectContent<T>(data, new System.Net.Http.Formatting.JsonMediaTypeFormatter())
            };
            return AddResponseHeaders(Request, responseMessage, null);
        }

        /// <summary>
        /// Returns an error message with the given message. This is returned to the client using the supplied status code
        /// </summary>
        /// <param name="request">Request message that is used to create output message.</param>
        /// <param name="statusCode">Status code to return to the client</param>
        /// <param name="data">Error response that is sent to the client.</param>
        /// <returns>HttpResponseMessage that wraps the given payload</returns>
        public static HttpResponseMessage CreateJabbrErrorMessage(this HttpRequestMessage request, HttpStatusCode statusCode, string message, string filenamePrefix)
        {
            var responseMessage = new HttpResponseMessage(statusCode)
            {
                Content = new StringContent(
                    System.Text.Json.JsonSerializer.Serialize(new ErrorModel { Message = message }),
                    System.Text.Encoding.UTF8,
                    "application/json")
            };

            return AddResponseHeaders(request, responseMessage, filenamePrefix);
        }
        /// <summary>
        /// Returns an error message with the given message. This is returned to the client using the supplied status code
        /// </summary>
        /// <param name="request">Request message that is used to create output message.</param>
        /// <param name="statusCode">Status code to return to the client</param>
        /// <param name="data">Error response that is sent to the client.</param>
        /// <returns>HttpResponseMessage that wraps the given payload</returns>
        public static HttpResponseMessage CreateJabbrErrorMessage(this HttpRequestMessage request, HttpStatusCode statusCode, string message)
        {
            var responseMessage = new HttpResponseMessage(statusCode)
            {
                Content = new StringContent(
                    JsonSerializer.Serialize(new ErrorModel { Message = message }),
                    System.Text.Encoding.UTF8,
                    "application/json")
            };

            return AddResponseHeaders(request, responseMessage, null);
        }

        private static HttpResponseMessage AddResponseHeaders(HttpRequestMessage request, HttpResponseMessage responseMessage, string filenamePrefix)
        {
            return AddDownloadHeader(request, responseMessage, filenamePrefix);
        }
        private static HttpResponseMessage AddDownloadHeader(HttpRequestMessage request, HttpResponseMessage responseMessage, string filenamePrefix)
        {
            var queryString = new QueryStringCollection(request.RequestUri);
            bool download;
            if (queryString.TryGetAndConvert<bool>("download", out download))
            {
                if (download)
                {
                    responseMessage.Content.Headers.ContentDisposition = new ContentDispositionHeaderValue("attachment") { FileName = filenamePrefix + ".json" };
                }
            }
            else
            {
                return new HttpResponseMessage(HttpStatusCode.BadRequest)
                {
                    Content = new ObjectContent<ErrorModel>(
                        new ErrorModel { Message = "Value for download was specified but cannot be converted to true or false." },
                        new System.Net.Http.Formatting.JsonMediaTypeFormatter(),
                        "application/json")
                };
            }

            return responseMessage;
        }

        /// <summary>
        /// Determines whether the specified request is local. 
        /// This seems like reverse engineering the actual implementation, so it might change in future.
        /// </summary>
        /// <param name="requestMessage">The request message.</param>
        /// <returns>
        ///   <c>true</c> if the specified request message is local; otherwise, <c>false</c>.
        /// </returns>
        public static bool IsLocal(this HttpRequestMessage requestMessage)
        {
            if (_httpContextAccessor?.HttpContext == null)
            {
                return false;
            }

            var connection = _httpContextAccessor.HttpContext.Connection;
            if (connection.RemoteIpAddress != null)
            {
                return connection.LocalIpAddress != null
                    ? connection.RemoteIpAddress.Equals(connection.LocalIpAddress)
                    : IPAddress.IsLoopback(connection.RemoteIpAddress);
            }

            // for in memory TestServer or when dealing with default connection info
            if (connection.RemoteIpAddress == null && connection.LocalIpAddress == null)
            {
                return true;
            }

            return false;
        }

        /// <summary>
        /// Sets IsLocal for the specified HttpRequestMessage
        /// Do not use outside of unit tests
        /// </summary>
        /// <param name="requestMessage">The request message.</param>
        /// <param name="value">New value of isLocal</param>
        public static void SetIsLocal(this HttpRequestMessage requestMessage, bool value)
        {
            // This method is no longer applicable in ASP.NET Core.
            // You may need to modify your unit tests to use a different approach for mocking IsLocal.
            throw new NotSupportedException("SetIsLocal is not supported in ASP.NET Core. Modify your tests to use IHttpContextAccessor for mocking.");
        }

        /// <summary>
        /// Gets the absolute URI of the current server, even if the app is running behind a load balancer.
        /// Taken from AppHarbour blog and adapted to use request protocol and for use with Web API.
        /// </summary>
        /// <param name="requestMessage">The request message.</param>
        /// <param name="relativeUri">The relative URI.</param>
        /// <returns></returns>
        public static Uri GetAbsoluteUri(this HttpRequestMessage requestMessage, string relativeUri)
        {
            var proto = "http";
            IEnumerable<string> headerValues;

            if (requestMessage.Headers.TryGetValues("X-Forwarded-Proto", out headerValues))
            {
                proto = headerValues.FirstOrDefault();
            }

            var uriBuilder = new UriBuilder
            {
                Host = requestMessage.RequestUri.Host,
                Path = "/",
                Scheme = proto,
            };

            uriBuilder.Port = requestMessage.RequestUri.Port;

            return new Uri(uriBuilder.Uri, relativeUri);
        }
    }
}
using Diplo.Translator.Models;
using Microsoft.AspNetCore.WebUtilities;
using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace Diplo.Translator.Services
{
    /// <summary>
    /// Simple HTTP service for posting data as JSON
    /// </summary>
    public class HttpJsonService : IHttpJsonService
    {
        private static readonly HttpClient client = new HttpClient();

        static HttpJsonService()
        {
            client.DefaultRequestHeaders.Accept.Clear();
            client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
            client.DefaultRequestHeaders.Add("User-Agent", "Diplo.Translator");
        }

        public HttpJsonService()
        {
        }

        /// <summary>
        /// Posts data as JSON and returns the results deserialised in a wrapper
        /// </summary>
        /// <typeparam name="D">The type of the data to be posted</typeparam>
        /// <typeparam name="R">The type of the response expected back</typeparam>
        /// <param name="endpoint">The endpoint URL to post the data to</param>
        /// <param name="data">The data to be posted. This is serialised to JSON.</param>
        /// <param name="queryParams">Optional query parameters to append to the endpoint</param>
        /// <param name="headers">Optional additional headers to add to the request</param>
        /// <returns>A response wrapped</returns>
        public async Task<ResponseWrapper<R>> Post<D, R>(string endpoint, D data, Dictionary<string, string> queryParams = null, Dictionary<string, string> headers = null)
            where D : class where R : class
        {
            if (string.IsNullOrEmpty(endpoint))
            {
                throw new ArgumentNullException(nameof(endpoint));
            }

            if (data is null)
            {
                throw new ArgumentNullException(nameof(data));
            }

            if (queryParams != null)
            {
                endpoint = QueryHelpers.AddQueryString(endpoint, queryParams);
            }

            using (var request = new HttpRequestMessage(HttpMethod.Post, endpoint))
            {
                if (headers != null)
                {
                    foreach (var header in headers)
                    {
                        request.Headers.Add(header.Key, header.Value);
                    }
                }

                var json = JsonSerializer.Serialize(data);

                request.Content = new StringContent(json, Encoding.UTF8, "application/json");

                var response = await client.SendAsync(request);

                var wrapper = new ResponseWrapper<R>()
                {
                    StatusCode = response.StatusCode,
                    IsSuccess = response.IsSuccessStatusCode
                };

                if (response.IsSuccessStatusCode)
                {
                    try
                    {
                        wrapper.Model = await response.Content.ReadFromJsonAsync<R>();
                        wrapper.Message = response.ReasonPhrase;
                    }
                    catch (Exception ex)
                    {
                        wrapper.IsSuccess = false;
                        wrapper.Message = ex.Message;
                    }
                }
                else
                {
                    wrapper.Message = await response.Content.ReadAsStringAsync();
                }

                return wrapper;
            }
        }
    }
}

using System;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using TwitterOAuth.RestAPI.Exceptions;

namespace TwitterFollowers.Console
{
    public class HttpHelper
    {
        private readonly HttpClient _httpClient;

        public HttpHelper()
        {
            _httpClient = new HttpClient();
        }

        public HttpHelper(HttpClient httpClient)
        {
            _httpClient = httpClient;
        }

        public async Task<T> HttpSend<T>(string authHeader, Uri uri, HttpMethod httpMethod = null)
        {
            if (httpMethod == null)
                httpMethod = HttpMethod.Get;

            _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("OAuth", authHeader);

            var request = new HttpRequestMessage
            {
                RequestUri = uri,
                Method = httpMethod
            };

            var responseMessage = await _httpClient.SendAsync(request).ConfigureAwait(false);

            if (responseMessage.IsSuccessStatusCode)
            {
                var response = await responseMessage.Content.ReadAsStringAsync().ConfigureAwait(false);

                if (JToken.Parse(response).Type == JTokenType.Array)
                {
                    var jArray = JArray.Parse(response);

                    var responseObject = JsonConvert.DeserializeObject<T>(jArray.ToString());
                    return responseObject;
                }
                else
                {
                    var jObject = JObject.Parse(response);

                    var responseObject = JsonConvert.DeserializeObject<T>(jObject.ToString());
                    return responseObject;
                }
            }

            if (responseMessage.StatusCode == (HttpStatusCode)429)
            {
                System.Console.WriteLine("Rate limited!");
                string rateLimit = Utils.GetHeaderValue(responseMessage.Headers, "X-Rate-Limit-Reset");
                throw new RateLimitedException(responseMessage.ReasonPhrase, Convert.ToInt64(rateLimit));
            }

            throw new Exception();
        }
    }
}

using System;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.Mime;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace DevStore.Core.Http
{
    public interface IRestClient
    {
        Task<TResult> PostAsync<T, TResult>(T @event, string token = null);
    }

    public class RestClient : IRestClient
    {
        private static JsonSerializerOptions _options = new() { PropertyNameCaseInsensitive = true };

        private readonly IHttpClientFactory _httpClientFactory;

        public RestClient(IHttpClientFactory httpClientFactory)
        {
            _httpClientFactory = httpClientFactory;
        }

        public async Task<TResult> PostAsync<T, TResult>(T @event, string token = null)
        {
            var content = new StringContent(JsonSerializer.Serialize(@event), Encoding.UTF8, MediaTypeNames.Application.Json);

            var httpRequestMessage = new HttpRequestMessage { Method = HttpMethod.Post, Content = content };

            if (!string.IsNullOrEmpty(token)) 
            {
                httpRequestMessage.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
            }

            var httpClient = _httpClientFactory.CreateClient(@event.GetType().Name);

            var responseMessage = await httpClient.SendAsync(httpRequestMessage);

            var response = await responseMessage.Content.ReadAsStringAsync();

            return JsonSerializer.Deserialize<TResult>(response, _options);
        }
    }
}
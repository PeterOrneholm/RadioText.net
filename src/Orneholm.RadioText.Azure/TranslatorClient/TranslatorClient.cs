using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace Orneholm.RadioText.Azure.TranslatorClient
{
    /// <summary>
    /// Based on https://docs.microsoft.com/en-au/azure/cognitive-services/translator/quickstart-translate
    /// </summary>
    public class TranslatorClient
    {
        private readonly HttpClient _httpClient;

        public TranslatorClient(HttpClient httpClient)
        {
            _httpClient = httpClient;
        }

        public static TranslatorClient CreateClient(string subscriptionKey, string endpoint)
        {
            var httpClient = new HttpClient
            {
                BaseAddress = new Uri(endpoint)
            };
            httpClient.DefaultRequestHeaders.Add("Ocp-Apim-Subscription-Key", subscriptionKey);
            return new TranslatorClient(httpClient);
        }

        public async Task<List<TranslationResult>> Translate(string text, string toLanguage, string? fromLanguage = null)
        {
            return await Translate(new List<string> { text }, new List<string> { toLanguage }, fromLanguage);
        }

        public async Task<List<TranslationResult>> Translate(List<string> texts, List<string> toLanguages, string? fromLanguage = null)
        {
            var urlQueryParams = GetUrlQueryParams(toLanguages, fromLanguage ?? string.Empty);
            var url = GetUrlWithQueryString("translate", urlQueryParams);

            var requestBodyContent = GetRequestBodyContent(texts);
            var response = await _httpClient.PostAsync(url, requestBodyContent).ConfigureAwait(false);
            var resultString = await response.Content.ReadAsStringAsync();
            var translationResult = JsonSerializer.Deserialize<List<TranslationResult>>(resultString, new JsonSerializerOptions()
            {
                PropertyNameCaseInsensitive = true
            });

            return translationResult;
        }

        private static StringContent GetRequestBodyContent(List<string> texts)
        {
            var requestBody = JsonSerializer.Serialize(texts.Select(x => new TranslationRequest(x)).ToList());

            return new StringContent(requestBody, Encoding.UTF8, "application/json");
        }

        private static List<KeyValuePair<string, string>> GetUrlQueryParams(List<string> toLanguages, string fromLanguage)
        {
            var urlQueryParams = new List<KeyValuePair<string, string>>
            {
                new KeyValuePair<string, string>("api-version", "3.0")
            };
            if (!string.IsNullOrWhiteSpace(fromLanguage))
            {
                urlQueryParams.Add(new KeyValuePair<string, string>("from", fromLanguage));
            }

            foreach (var toLanguage in toLanguages)
            {
                urlQueryParams.Add(new KeyValuePair<string, string>("to", toLanguage));
            }

            return urlQueryParams;
        }

        private static string GetUrlWithQueryString(string baseUrl, List<KeyValuePair<string, string>> urlQueryParams)
        {
            return baseUrl + "?" + string.Join("&", urlQueryParams.Select(x => $"{x.Key}={Uri.EscapeUriString(x.Value)}"));
        }
    }
}

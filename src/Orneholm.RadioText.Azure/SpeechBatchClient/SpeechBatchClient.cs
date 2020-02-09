using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text.Json;
using System.Threading.Tasks;

namespace Orneholm.RadioText.Azure.SpeechBatchClient
{
    /// <summary>
    /// Source: https://github.com/Azure-Samples/cognitive-services-speech-sdk/blob/master/samples/batch/csharp/batchclient.cs
    /// </summary>
    public class SpeechBatchClient : ISpeechBatchClient
    {
        private const string OneApiOperationLocationHeaderKey = "Operation-Location";
        private const string SpeechToTextBasePath = "api/speechtotext/v2.0/";

        private readonly HttpClient _client;

        private SpeechBatchClient(HttpClient client)
        {
            _client = client;
        }

        public static SpeechBatchClient CreateApiV2Client(string key, string hostName, int port)
        {
            var client = new HttpClient
            {
                Timeout = TimeSpan.FromMinutes(25),
                BaseAddress = new UriBuilder(Uri.UriSchemeHttps, hostName, port).Uri
            };

            client.DefaultRequestHeaders.Add("Ocp-Apim-Subscription-Key", key);

            return new SpeechBatchClient(client);
        }

        public Task<IEnumerable<Transcription>> GetTranscriptionsAsync()
        {
            return GetAsync<IEnumerable<Transcription>>($"{SpeechToTextBasePath}Transcriptions");
        }

        public Task<Transcription> GetTranscriptionAsync(Guid id)
        {
            return GetAsync<Transcription>($"{SpeechToTextBasePath}Transcriptions/{id}");
        }

        public Task<Uri> PostTranscriptionAsync(TranscriptionDefinition transcriptionDefinition)
        {
            var path = $"{SpeechToTextBasePath}Transcriptions/";
            return PostAsJsonAsync(path, transcriptionDefinition);
        }

        public Task<Uri> PostTranscriptionAsync(string name, string description, string locale, Uri recordingsUrl)
        {
            var path = $"{SpeechToTextBasePath}Transcriptions/";
            var transcriptionDefinition = TranscriptionDefinition.Create(name, description, locale, recordingsUrl);

            return PostAsJsonAsync(path, transcriptionDefinition);
        }

        public Task<Uri> PostTranscriptionAsync(string name, string description, string locale, Uri recordingsUrl, IEnumerable<Guid> modelIds)
        {
            var modelIdsList = modelIds.ToList();
            if (!modelIdsList.Any())
            {
                return PostTranscriptionAsync(name, description, locale, recordingsUrl);
            }

            var models = modelIdsList.Select(ModelIdentity.Create).ToList();
            var path = $"{SpeechToTextBasePath}Transcriptions/";

            var transcriptionDefinition = TranscriptionDefinition.Create(name, description, locale, recordingsUrl, models);
            return PostAsJsonAsync(path, transcriptionDefinition);
        }

        public Task<Transcription> GetTranscriptionAsync(Uri location)
        {
            if (location == null)
            {
                throw new ArgumentNullException(nameof(location));
            }

            return GetAsync<Transcription>(location.AbsolutePath);
        }

        public Task DeleteTranscriptionAsync(Guid id)
        {
            return _client.DeleteAsync($"{SpeechToTextBasePath}Transcriptions/{id}");
        }

        private static async Task<Uri> GetLocationFromPostResponseAsync(HttpResponseMessage response)
        {
            if (!response.IsSuccessStatusCode)
            {
                throw await CreateExceptionAsync(response).ConfigureAwait(false);
            }

            IEnumerable<string> headerValues;
            if (response.Headers.TryGetValues(OneApiOperationLocationHeaderKey, out headerValues))
            {
                var headerValuesList = headerValues.ToList();
                if (headerValuesList.Any())
                {
                    return new Uri(headerValuesList.First());
                }
            }

            return response.Headers.Location;
        }

        private async Task<Uri> PostAsJsonAsync<TPayload>(string path, TPayload payload)
        {

            var res = JsonSerializer.Serialize(payload);
            var sc = new StringContent(res);
            sc.Headers.ContentType = MediaTypeHeaderValue.Parse("application/json");

            using (var response = await _client.PostAsync(path, sc))
            {
                return await GetLocationFromPostResponseAsync(response).ConfigureAwait(false);
            }
        }

        private async Task<TResponse> GetAsync<TResponse>(string path)
        {
            using (var response = await _client.GetAsync(path).ConfigureAwait(false))
            {
                var contentType = response.Content.Headers.ContentType;

                if (response.IsSuccessStatusCode && string.Equals(contentType.MediaType, "application/json", StringComparison.OrdinalIgnoreCase))
                {
                    var content = await response.Content.ReadAsStringAsync();
                    var result = JsonSerializer.Deserialize<TResponse>(content, new JsonSerializerOptions()
                    {
                        PropertyNameCaseInsensitive = true
                    });

                    return result;
                }

                throw new NotImplementedException();
            }
        }

        private static async Task<FailedHttpClientRequestException> CreateExceptionAsync(HttpResponseMessage response)
        {
            switch (response.StatusCode)
            {
                case HttpStatusCode.Forbidden:
                    return new FailedHttpClientRequestException(response.StatusCode, "No permission to access this resource.");
                case HttpStatusCode.Unauthorized:
                    return new FailedHttpClientRequestException(response.StatusCode, "Not authorized to see the resource.");
                case HttpStatusCode.NotFound:
                    return new FailedHttpClientRequestException(response.StatusCode, "The resource could not be found.");
                case HttpStatusCode.UnsupportedMediaType:
                    return new FailedHttpClientRequestException(response.StatusCode, "The file type isn't supported.");
                case HttpStatusCode.BadRequest:
                    {
                        var content = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
                        var result = JsonSerializer.Deserialize<BadRequestResult>(content);
                        if (result != null && !string.IsNullOrEmpty(result.Message))
                        {
                            return new FailedHttpClientRequestException(response.StatusCode, result.Message);
                        }

                        return new FailedHttpClientRequestException(response.StatusCode, response.ReasonPhrase);
                    }

                default:
                    return new FailedHttpClientRequestException(response.StatusCode, response.ReasonPhrase);
            }
        }

        private class BadRequestResult
        {
            public string Message { get; set; } = string.Empty;
        }
    }
}

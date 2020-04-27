using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Azure.Storage.Blob;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Orneholm.RadioText.Azure.SpeechBatchClient;
using Orneholm.RadioText.Core.Storage;

namespace Orneholm.RadioText.Core
{
    public class SrEpisodeTranscriber
    {
        private const string StatusTranscribing = "Transcribing";
        private const string StatusTranscribed = "Transcribed";

        private static readonly TimeSpan WaitBetweenStatusCheck = TimeSpan.FromSeconds(30);

        private readonly string _transcriptionsContainerName;
        private readonly ISpeechBatchClientFactory _speechBatchClientFactory;
        private readonly IStorageTransfer _storageTransfer;
        private readonly ILogger<SrEpisodeCollector> _logger;
        private readonly IStorage _storage;
        private readonly CloudBlobClient _cloudBlobClient;

        public SrEpisodeTranscriber(string transcriptionsContainerName, ISpeechBatchClientFactory speechBatchClientFactory, IStorageTransfer storageTransfer, ILogger<SrEpisodeCollector> logger, IStorage storage, CloudBlobClient cloudBlobClient)
        {
            _transcriptionsContainerName = transcriptionsContainerName;
            _speechBatchClientFactory = speechBatchClientFactory;
            _storageTransfer = storageTransfer;
            _logger = logger;
            _storage = storage;
            _cloudBlobClient = cloudBlobClient;
        }

        public async Task TranscribeAndPersist(int episodeId)
        {
            var storedEpisode = await _storage.GetEpisode(episodeId);
            if (storedEpisode == null)
            {
                _logger.LogWarning($"Episode {episodeId} isn't available...");
                return;
            }

            var storedEpisodeTranscription = await _storage.GetEpisodeTranscription(episodeId);
            if (storedEpisodeTranscription != null && storedEpisodeTranscription.Status == StatusTranscribed)
            {
                _logger.LogInformation($"Episode {storedEpisode.Episode.Id} already transcribed...");
                return;
            }

            await _storage.StoreTranscription(episodeId, new SrStoredEpisodeTranscription
            {
                EpisodeId = episodeId,
                Status = StatusTranscribing
            });

            var speechBatchClient = _speechBatchClientFactory.Get();

            await TranscribeAndPersist(storedEpisode, speechBatchClient);
        }

        private async Task TranscribeAndPersist(SrStoredEpisode storedEpisode, SpeechBatchClient speechBatchClient)
        {
            _logger.LogInformation($"Transcribing episode {storedEpisode.Episode.Id}...");
            var episodeTranscriptionId = await TranscribeEpisode(storedEpisode, speechBatchClient);
            var episodeTranscription = await WaitForTranscription(episodeTranscriptionId, storedEpisode, speechBatchClient);
            _logger.LogInformation($"Transcribed episode {storedEpisode.Episode.Id}...");

            if (episodeTranscription == null)
            {
                return;
            }

            _logger.LogInformation($"Transfer transcribed episode {storedEpisode.Episode.Id}...");
            var storedEpisodeTranscription = await TransferTranscribedEpisode(episodeTranscription, storedEpisode);
            await speechBatchClient.DeleteTranscriptionAsync(episodeTranscriptionId);

            var transcriptionResult = await GetTranscriptionResult(storedEpisodeTranscription, storedEpisodeTranscription.TranscriptionResultChannel0BlobIdentifier);
            await StoreTranscriptionResult(storedEpisode, transcriptionResult, storedEpisodeTranscription);
            _logger.LogInformation($"Transfered transcribed episode {storedEpisode.Episode.Id}...");
        }

        private async Task StoreTranscriptionResult(SrStoredEpisode storedEpisode, TranscriptionResult transcriptionResult, SrStoredEpisodeTranscription storedEpisodeTranscription)
        {
            var combinedResult = transcriptionResult.AudioFileResults.FirstOrDefault()?.CombinedResults.FirstOrDefault();
            storedEpisodeTranscription.CombinedDisplayResult = combinedResult?.Display ?? string.Empty;
            await _storage.StoreTranscription(storedEpisode.Episode.Id, storedEpisodeTranscription);
        }

        private async Task<TranscriptionResult> GetTranscriptionResult(SrStoredEpisodeTranscription storedEpisodeTranscription, string blobName)
        {
            var transcriptionsContainer = _cloudBlobClient.GetContainerReference(_transcriptionsContainerName);
            var transcriptionBlob = transcriptionsContainer.GetBlockBlobReference(blobName);
            var transcriptionBlobContent = await transcriptionBlob.DownloadTextAsync();

            return JsonConvert.DeserializeObject<TranscriptionResult>(transcriptionBlobContent);
        }

        private async Task<Guid> TranscribeEpisode(SrStoredEpisode storedEpisode, SpeechBatchClient speechBatchClient)
        {
            var audioUrl = RemoveQueryString(storedEpisode.AudioUrl);
            var transcriptionDefinition = TranscriptionDefinition.Create(
                $"RadioText - Episode {storedEpisode.Episode.Id}",
                "RadioText",
                storedEpisode.AudioLocale,
                new Uri(audioUrl)
            );

            var transcriptionLocation = await speechBatchClient.PostTranscriptionAsync(transcriptionDefinition);
            return GetTranscriptionGuid(transcriptionLocation);
        }

        private string RemoveQueryString(string url)
        {
            var questionMarkIndex = url.IndexOf("?");
            if (questionMarkIndex > 0)
            {
                return url.Substring(0, questionMarkIndex);
            }

            return url;
        }

        private static Guid GetTranscriptionGuid(Uri transcriptionLocation)
        {
            return new Guid(transcriptionLocation.ToString().Split('/').LastOrDefault() ?? string.Empty);
        }

        private async Task<Transcription?> WaitForTranscription(Guid transcriptionId, SrStoredEpisode storedEpisode, SpeechBatchClient speechBatchClient)
        {
            while (true)
            {
                var transcription = await speechBatchClient.GetTranscriptionAsync(transcriptionId);

                _logger.LogTrace($"Transcribing status for {storedEpisode.Episode.Id} is {transcription.Status}");

                switch (transcription.Status)
                {
                    case "":
                    case "Failed":
                        _logger.LogError($"Error transcribing {storedEpisode.Episode.Id}: {transcription.StatusMessage}");
                        throw new Exception($"Error transcribing {storedEpisode.Episode.Id}: {transcription.StatusMessage}");
                    case "Succeeded":
                        _logger.LogInformation($"Transcribed {storedEpisode.Episode.Id}");
                        return transcription;
                    case "NotStarted":
                    case "Running":
                        break;
                }

                await Task.Delay(WaitBetweenStatusCheck);
            }
        }

        private async Task<SrStoredEpisodeTranscription> TransferTranscribedEpisode(Azure.SpeechBatchClient.Transcription transcription, SrStoredEpisode storedEpisode)
        {
            var channel0 = await TransferResultForChannel(storedEpisode, transcription, "0");
            var channel1 = await TransferResultForChannel(storedEpisode, transcription, "1");

            return new SrStoredEpisodeTranscription
            {
                Status = StatusTranscribed,

                TranscriptionResultChannel0BlobIdentifier = channel0?.blobIdentifier ?? string.Empty,
                TranscriptionResultChannel0Url = channel0?.blobUri.ToString() ?? string.Empty,

                TranscriptionResultChannel1BlobIdentifier = channel1?.blobIdentifier ?? string.Empty,
                TranscriptionResultChannel1Url = channel1?.blobUri.ToString() ?? string.Empty,
            };
        }

        private async Task<(string blobIdentifier, Uri blobUri)?> TransferResultForChannel(SrStoredEpisode storedEpisode, Azure.SpeechBatchClient.Transcription transcription, string channel)
        {
            if (!transcription.ResultsUrls.ContainsKey($"channel_{channel}"))
            {
                return null;
            }

            var targetBlobPrefix = storedEpisode.AudioBlobIdentifier + "__Transcription_";
            var targetBlobIdentifier = $"{targetBlobPrefix}{channel}.json";
            var resultsUri = transcription.ResultsUrls[$"channel_{channel}"];

            var targetBlobUrl = await _storageTransfer.TransferBlockBlobIfNotExists(
                _transcriptionsContainerName,
                targetBlobIdentifier,
                resultsUri
            );

            return (targetBlobIdentifier, targetBlobUrl);
        }
    }
}

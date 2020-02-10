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

        private static readonly TimeSpan WaitBetweenStatusCheck = TimeSpan.FromSeconds(5);

        private readonly string _transcriptionsContainerName;
        private readonly IStorageTransfer _storageTransfer;
        private readonly ILogger<SrEpisodeCollector> _logger;
        private readonly IStorage _storage;
        private readonly SpeechBatchClient _speechBatchClient;
        private readonly CloudBlobClient _cloudBlobClient;

        public SrEpisodeTranscriber(string transcriptionsContainerName, SpeechBatchClient speechBatchClient, IStorageTransfer storageTransfer, ILogger<SrEpisodeCollector> logger, IStorage storage, CloudBlobClient cloudBlobClient)
        {
            _transcriptionsContainerName = transcriptionsContainerName;
            _storageTransfer = storageTransfer;
            _logger = logger;
            _storage = storage;
            _cloudBlobClient = cloudBlobClient;
            _speechBatchClient = speechBatchClient;
        }

        public async Task TranscribeAndPersist(int episodeId)
        {
            var storedEpisode = await _storage.GetEpisode(episodeId);
            if (storedEpisode == null)
            {
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

            await TranscribeAndPersist(storedEpisode);
        }

        private async Task TranscribeAndPersist(SrStoredEpisode storedEpisode)
        {
            _logger.LogInformation($"Transcribing episode {storedEpisode.Episode.Id}...");
            var episodeTranscriptionId = await TranscribeEpisode(storedEpisode);
            var episodeTranscription = await WaitForTranscription(episodeTranscriptionId, storedEpisode);
            _logger.LogInformation($"Transcribed episode {storedEpisode.Episode.Id}...");

            if (episodeTranscription == null)
            {
                return;
            }

            _logger.LogInformation($"Transfer transcribed episode {storedEpisode.Episode.Id}...");
            var storedEpisodeTranscription = await TransferTranscribedEpisode(episodeTranscription, storedEpisode);
            await _speechBatchClient.DeleteTranscriptionAsync(episodeTranscriptionId);

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

        private async Task<Guid> TranscribeEpisode(SrStoredEpisode storedEpisode)
        {
            var transcriptionDefinition = TranscriptionDefinition.Create(
                $"RadioText - Episode {storedEpisode.Episode.Id}",
                "RadioText",
                storedEpisode.AudioLocale,
                new Uri(storedEpisode.AudioUrl)
            );

            var transcriptionLocation = await _speechBatchClient.PostTranscriptionAsync(transcriptionDefinition);
            return GetTranscriptionGuid(transcriptionLocation);
        }

        private static Guid GetTranscriptionGuid(Uri transcriptionLocation)
        {
            return new Guid(transcriptionLocation.ToString().Split('/').LastOrDefault() ?? string.Empty);
        }

        private async Task<Azure.SpeechBatchClient.Transcription?> WaitForTranscription(Guid transcriptionId, SrStoredEpisode storedEpisode)
        {
            while (true)
            {
                var transcription = await _speechBatchClient.GetTranscriptionAsync(transcriptionId);
                switch (transcription.Status)
                {
                    case "":
                    case "Failed":
                        _logger.LogError($"Error transcribing {storedEpisode.Episode.Id}");
                        return null;
                    case "Succeeded":
                        _logger.LogInformation($"Transcribed {storedEpisode.Episode.Id}");
                        return transcription;
                    case "NotStarted":
                    case "Running":
                        continue;
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

        public async Task CleanExistingTranscriptions()
        {
            var transcriptions = await _speechBatchClient.GetTranscriptionsAsync();
            foreach (var transcription in transcriptions)
            {
                await _speechBatchClient.DeleteTranscriptionAsync(transcription.Id);
            }
        }
    }
}

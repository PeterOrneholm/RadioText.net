using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Orneholm.RadioText.Core.Storage;
using Orneholm.SverigesRadio.Api;
using Orneholm.SverigesRadio.Api.Models.Request.Common;
using Orneholm.SverigesRadio.Api.Models.Request.Episodes;
using Orneholm.SverigesRadio.Api.Models.Response.Episodes;

namespace Orneholm.RadioText.Core
{
    public class SrEpisodeCollector
    {
        private readonly string _cloudBlobContainerName;
        private readonly ISverigesRadioApiClient _sverigesRadioApiClient;
        private readonly ILogger<SrEpisodeCollector> _logger;
        private readonly IStorageTransfer _storageTransfer;
        private readonly IStorage _storage;
        private readonly HttpClient _httpClientNoRedirect;

        public SrEpisodeCollector(string cloudBlobContainerName, IStorageTransfer storageTransfer, ISverigesRadioApiClient sverigesRadioApiClient, ILogger<SrEpisodeCollector> logger, IStorage storage)
        {
            _cloudBlobContainerName = cloudBlobContainerName;
            _sverigesRadioApiClient = sverigesRadioApiClient;
            _logger = logger;
            _storage = storage;
            _storageTransfer = storageTransfer;

            _httpClientNoRedirect = new HttpClient(new HttpClientHandler
            {
                AllowAutoRedirect = false
            });
        }

        public async Task<SrStoredEpisode?> Collect(int episodeId)
        {
            var episode = await GetSrEpisode(episodeId);

            var fileUrl = GetFileUrl(episode);
            if (fileUrl == null)
            {
                return null;
            }

            _logger.LogInformation($"Collecting SR episode {episode.Id}");
            var storedEpisode = await _storage.GetEpisode(episode.Id);
            if (storedEpisode != null)
            {
                _logger.LogInformation($"SR episode {episode.Id} was already collected");
                return storedEpisode;
            }

            storedEpisode = await GetStoredEpisodeModel(fileUrl, episode);
            var transferAudioBlockBlob = await _storageTransfer.TransferBlockBlobIfNotExists(_cloudBlobContainerName, storedEpisode.AudioBlobIdentifier, storedEpisode.OriginalAudioUrl);
            storedEpisode.AudioUrl = transferAudioBlockBlob.ToString();

            await _storageTransfer.TransferBlockBlobIfNotExists(_cloudBlobContainerName, storedEpisode.ImageBlobIdentifier, storedEpisode.OriginalAudioUrl);

            await _storage.StoreEpisode(episode.Id, storedEpisode);

            _logger.LogInformation($"Collected SR episode {episode.Id}");

            return storedEpisode;
        }

        private async Task<SrStoredEpisode> GetStoredEpisodeModel(string fileUrl, Episode episode)
        {
            var audioFinalUri = await GetUriAfterOneRedirect(fileUrl);
            if (audioFinalUri.Host == "")
            {
                audioFinalUri = new Uri((new Uri(fileUrl)).Host + audioFinalUri);
            }
            var audioFinalUrl = audioFinalUri.ToString();
            var audioExtension = audioFinalUrl.Substring(audioFinalUrl.LastIndexOf('.') + 1);
            var imageExtension = episode.ImageUrl.Substring(episode.ImageUrl.LastIndexOf('.') + 1);

            return new SrStoredEpisode
            {
                Episode = episode,
                OriginalAudioUrl = audioFinalUrl,

                AudioBlobIdentifier = GetBlobName(episode.Program.Id, episode, audioExtension, "Audio"),
                ImageBlobIdentifier = GetBlobName(episode.Program.Id, episode, imageExtension, "Thumbnail"),

                AudioExtension = audioExtension,
                AudioLocale = GetAudioLocaleForEpisode(episode)
            };
        }

        private static string GetBlobName(int programId, Episode episode, string extension, string type)
        {
            return $"SR/programs/{programId}/episodes/{episode.Id}/SR_{programId}__{episode.PublishDateUtc:yyyy-MM-dd}_{episode.PublishDateUtc:HH-mm}__{episode.Id}__{type}.{extension}";
        }

        private async Task<Episode> GetSrEpisode(int episodeId)
        {
            var episodeResult = await _sverigesRadioApiClient.GetEpisodeAsync(new EpisodeDetailsRequest(episodeId)
            {
                AudioSettings = new AudioSettings
                {
                    AudioQuality = AudioQuality.High,
                    OnDemandAudioTemplateId = SverigesRadioApiIds.OnDemandAudioTemplates.M4A_M3U8
                }
            });

            return episodeResult.Episode;
        }

        private async Task<Uri> GetUriAfterOneRedirect(string url)
        {
            var httpResult = await _httpClientNoRedirect.GetAsync(url);
            if (httpResult.StatusCode == HttpStatusCode.Redirect
                || httpResult.StatusCode == HttpStatusCode.Moved
                || httpResult.StatusCode == HttpStatusCode.TemporaryRedirect)
            {
                return httpResult.Headers.Location;
            }

            return new Uri(url);
        }

        private static string ProgramLocaleDefault = "sv-SE";
        private static readonly Dictionary<int, string> ProgramLocaleMapping = new Dictionary<int, string>
        {
            { SverigesRadioApiIds.Programs.RadioSweden, "en-US" }, // Radio Sweden - English
            { 2494, "ar-EG" } // Radio Sweden - Arabic
        };

        private static string GetAudioLocaleForEpisode(Episode episode)
        {
            var programId = episode.Program.Id;
            if (ProgramLocaleMapping.ContainsKey(programId))
            {
                return ProgramLocaleMapping[programId];
            }

            return ProgramLocaleDefault;
        }

        private static string? GetFileUrl(Episode episode)
        {
            var fileUrl = episode?.DownloadPodfile?.Url;
            fileUrl ??= episode?.Broadcast?.BroadcastFiles.FirstOrDefault()?.Url;

            return fileUrl;
        }
    }
}

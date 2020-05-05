using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
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
        private readonly string _ffMpegLocation;
        private readonly HttpClient _httpClientNoRedirect;

        public SrEpisodeCollector(string cloudBlobContainerName, IStorageTransfer storageTransfer, ISverigesRadioApiClient sverigesRadioApiClient, ILogger<SrEpisodeCollector> logger, IStorage storage, string ffMpegLocation)
        {
            _cloudBlobContainerName = cloudBlobContainerName;
            _sverigesRadioApiClient = sverigesRadioApiClient;
            _logger = logger;
            _storage = storage;
            _ffMpegLocation = ffMpegLocation;
            _storageTransfer = storageTransfer;

            _httpClientNoRedirect = new HttpClient(new HttpClientHandler
            {
                AllowAutoRedirect = false
            });
        }

        public async Task Collect(int episodeId)
        {
            _logger.LogInformation($"Collecting SR episode {episodeId}");
            var storedEpisode = await _storage.GetEpisode(episodeId);
            if (storedEpisode != null)
            {
                _logger.LogInformation($"SR episode {episodeId} was already collected");
                return;
            }

            var episode = await GetSrEpisode(episodeId);

            var fileUrl = GetFileUrl(episode);
            if (fileUrl == null)
            {
                throw new Exception($"SR episode {episodeId} does not have any available broadcast file, can't collect");
            }

            storedEpisode = await GetStoredEpisodeModel(fileUrl, episode);
            var audioStream = await GetOnlyAudioStream(storedEpisode.OriginalAudioUrl);
            if (audioStream == null)
            {
                var transferAudioBlockBlob = await _storageTransfer.TransferBlockBlobAndOverwrite(_cloudBlobContainerName, storedEpisode.AudioBlobIdentifier, storedEpisode.OriginalAudioUrl, GetContentType(storedEpisode.AudioBlobIdentifier));
                storedEpisode.AudioUrl = transferAudioBlockBlob.ToString();
            }
            else
            {
                var uploadedAudioBlockBlob = await _storageTransfer.UploadBlockBlobAndOverwrite(_cloudBlobContainerName, storedEpisode.AudioBlobIdentifier, audioStream, GetContentType(storedEpisode.AudioBlobIdentifier));
                storedEpisode.AudioUrl = uploadedAudioBlockBlob.ToString();
            }

            await _storageTransfer.TransferBlockBlobIfNotExists(_cloudBlobContainerName, storedEpisode.ImageBlobIdentifier, storedEpisode.Episode.ImageUrlTemplate, GetContentType(storedEpisode.ImageBlobIdentifier));

            await _storage.StoreEpisode(episode.Id, storedEpisode);

            _logger.LogInformation($"Collected SR episode {episode.Id}");
        }

        private string? GetContentType(string blobIdentifier)
        {
            if (blobIdentifier.EndsWith(".m4a"))
            {
                return "audio/m4a";
            }

            if (blobIdentifier.EndsWith(".mp3"))
            {
                return "audio/mpeg";
            }

            if (blobIdentifier.EndsWith(".jpg") || blobIdentifier.EndsWith(".jpeg"))
            {
                return "image/jpeg";
            }

            if (blobIdentifier.EndsWith(".png"))
            {
                return "image/png";
            }

            return null;
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
            var imageExtension = episode.ImageUrl.Substring(episode.ImageUrl.LastIndexOf('.') + 1).Split('?')[0];

            return new SrStoredEpisode
            {
                Episode = episode,
                OriginalAudioUrl = audioFinalUrl,

                AudioBlobIdentifier = GetBlobName(episode.Program.Id, episode, audioExtension, "OriginalAudio"),
                ImageBlobIdentifier = GetBlobName(episode.Program.Id, episode, imageExtension, "OriginalImage"),

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
            { SrProgramIds.RadioSweden_English, "en-US" },
            { SrProgramIds.RadioSweden_Arabic, "ar-EG" },
            { SrProgramIds.RadioSweden_Finnish, "fi-FI" }
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
            var fileUrl = episode?.Broadcast?.BroadcastFiles.FirstOrDefault()?.Url;
            fileUrl ??= episode?.DownloadPodfile?.Url;

            return fileUrl;
        }


        private async Task<Stream?> GetOnlyAudioStream(string url)
        {
            if (string.IsNullOrWhiteSpace(_ffMpegLocation))
            {
                return null;
            }

            var tempFile = Path.GetTempFileName() + ".mp3";
            var ffMpegCommand = $"-i \"{url}\" -map 0:a -codec:a copy \"{tempFile}\"";
            var result = await ProcessAsyncHelper.ExecuteShellCommand(_ffMpegLocation, ffMpegCommand, 60000);

            if (result.ExitCode != 0)
            {
                return null;
            }

            var stream = new MemoryStream(File.ReadAllBytes(tempFile));
            stream.Seek(0, SeekOrigin.Begin);
            File.Delete(tempFile);

            return stream;
        }
    }
}

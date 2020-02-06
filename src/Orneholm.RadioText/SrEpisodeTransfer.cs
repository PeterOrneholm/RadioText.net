using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using Orneholm.SverigesRadio.Api;
using Orneholm.SverigesRadio.Api.Models.Request;
using Orneholm.SverigesRadio.Api.Models.Response.Episodes;

namespace Orneholm.NewsSearch
{
    public class SrEpisodeTransfer
    {
        private readonly string _cloudBlobContainerName;
        private readonly ISverigesRadioApiClient _sverigesRadioApiClient;
        private readonly HttpClient _httpClientNoRedirect;
        private readonly StorageTransfer _storageTransfer;

        private static string ProgramLocaleDefault = "sv-SE";
        private static readonly Dictionary<int, string> ProgramLocaleMapping = new Dictionary<int, string>
        {
            { SverigesRadioApiIds.Programs.RadioSweden, "en-US" }
        };

        public SrEpisodeTransfer(string cloudBlobContainerName, StorageTransfer storageTransfer, ISverigesRadioApiClient sverigesRadioApiClient)
        {
            _cloudBlobContainerName = cloudBlobContainerName;
            _sverigesRadioApiClient = sverigesRadioApiClient;
            _httpClientNoRedirect = new HttpClient(new HttpClientHandler
            {
                AllowAutoRedirect = false
            });

            _storageTransfer = storageTransfer;
        }

        public async Task<List<TransferedEpisode>> TransferSrEpisodes(int programId, int count)
        {
            Console.WriteLine($"Fetching episodes from SR (program {programId})...");
            var srEpisodes = await GetSrEpisodes(programId, count);
            Console.WriteLine($"Fetched {srEpisodes.Count} episodes from SR (program {programId})!");

            var transferedEpisodes = new List<TransferedEpisode>();

            foreach (var episode in srEpisodes)
            {
                var fileUrl = GetFileUrl(episode);

                if (fileUrl != null)
                {
                    var finalUri = await GetUriAfterOneRedirect(fileUrl);
                    if (finalUri.Host == "")
                    {
                        finalUri = new Uri((new Uri(fileUrl)).Host + finalUri);
                    }
                    var finalUrl = finalUri.ToString();
                    var extension = finalUrl.Substring(finalUrl.LastIndexOf('.') + 1);

                    var name = $"SR/{programId}/SR_{programId}__{episode.PublishDateUtc:yyyy-MM-dd}_{episode.PublishDateUtc:HH-mm}__{episode.Id}.{extension}";
                    transferedEpisodes.Add(new TransferedEpisode
                    {
                        Episode = episode,
                        EpisodeAudioLocale = GetAudioLocaleForEpisode(episode),
                        EpisodeBlobIdentifier = name,
                        OriginalAudioUri = finalUri,
                        OriginalAudioExtension = extension
                    });
                }
            }

            var blobs = transferedEpisodes.Select(x => new TransferBlob
            {
                TargetBlobIdentifier = x.EpisodeBlobIdentifier,
                SourceUrl = x.OriginalAudioUri.ToString(),
                TargetBlobMetadata = GetEpisodeMetadata(x.Episode)
            }).ToList();
            var transferBlockBlobs = await _storageTransfer.TransferBlockBlobs(_cloudBlobContainerName, blobs);

            foreach (var transferedEpisode in transferedEpisodes)
            {
                var transferBlockBlobUri = transferBlockBlobs[transferedEpisode.EpisodeBlobIdentifier];
                transferedEpisode.BlobAudioAuthenticatedUri = transferBlockBlobUri;
                transferedEpisode.BlobAudioUri = new Uri(transferBlockBlobUri.ToString().Split('?')[0]);
            }

            return transferedEpisodes;
        }

        private static string GetAudioLocaleForEpisode(Episode episode)
        {
            var programId = episode.Program.Id;
            if (ProgramLocaleMapping.ContainsKey(programId))
            {
                return ProgramLocaleMapping[programId];
            }

            return ProgramLocaleDefault;
        }

        private static string GetFileUrl(Episode episode)
        {
            var fileUrl = episode?.DownloadPodfile?.Url;
            fileUrl ??= episode?.Broadcast?.BroadcastFiles.FirstOrDefault()?.Url;
            return fileUrl;
        }

        private async Task<List<Episode>> GetSrEpisodes(int programId, int count)
        {
            var episodesResult = await _sverigesRadioApiClient.ListEpisodesAsync(programId, pagination: ListPagination.TakeFirst(count));

            return episodesResult.Episodes;
        }

        private async Task<Uri> GetUriAfterOneRedirect(string url)
        {
            var httpResult = await _httpClientNoRedirect.GetAsync(url);
            if (httpResult.StatusCode == HttpStatusCode.Redirect
                || httpResult.StatusCode == HttpStatusCode.PermanentRedirect)
            {
                return httpResult.Headers.Location;
            }

            return new Uri(url);
        }

        public static Dictionary<string, string> GetEpisodeMetadata(Episode episode)
        {
            return new Dictionary<string, string>
            {
                { "NS_Episode_Program_Id", episode.Program.Id.ToString() },
                { "NS_Episode_Program_Name", episode.Program.Name },
                { "NS_Episode_Id", episode.Id.ToString() },
                { "NS_Episode_WebUrl", episode.Url },
                { "NS_Episode_ImageUrl", episode.ImageUrl },
                { "NS_Episode_AudioUrl", GetFileUrl(episode) },
                { "NS_Episode_AudioLocale", GetAudioLocaleForEpisode(episode) },
                { "NS_Episode_Title_B64", GetBase64Encoded(episode.Title) },
                { "NS_Episode_Description_B64", GetBase64Encoded(episode.Description) },
                { "NS_Episode_PublishDateUtc", episode.PublishDateUtc.ToString("yyyy-MM-dd HH:mm") },
            };
        }

        private static string GetBase64Encoded(string text)
        {
            var encodedBytes = System.Text.Encoding.Unicode.GetBytes(text);
            return Convert.ToBase64String(encodedBytes);
        }
    }
}

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Orneholm.RadioText.Core.Storage;
using Orneholm.SverigesRadio.Api;
using Orneholm.SverigesRadio.Api.Models.Request;
using Orneholm.SverigesRadio.Api.Models.Response.Episodes;

namespace Orneholm.RadioText.Core.SverigesRadio
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

        public async Task<List<SrStoredEpisode>> Collect(List<int> programIds, int count)
        {
            var srStoredEpisodes = new ConcurrentBag<SrStoredEpisode>();
            var tasks = new List<Task>();

            foreach (var programId in programIds)
            {
                tasks.Add(Collect(programId, count).ContinueWith(x =>
                {
                    foreach (var srStoredEpisode in x.Result)
                    {
                        srStoredEpisodes.Add(srStoredEpisode);
                    }
                }));
            }

            await Task.WhenAll(tasks);

            return srStoredEpisodes.ToList();
        }

        public async Task<List<SrStoredEpisode>> Collect(int programId, int count)
        {
            var srEpisodes = await GetSrEpisodes(programId, count);

            var storedEpisodes = new ConcurrentBag<SrStoredEpisode>();
            var tasks = new List<Task>();

            foreach (var episode in srEpisodes)
            {
                var fileUrl = SrEpisodeMetadata.GetFileUrl(episode);

                if (fileUrl != null)
                {
                    var task = Task.Run(async () =>
                    {
                        _logger.LogInformation( $"Collecting SR episode {episode.Id}");
                        if (await _storage.EpisodeExists(episode.Program.Id, episode.Id))
                        {
                            _logger.LogInformation($"SR episode {episode.Id} was already collected");
                            return;
                        }

                        var storedEpisode = await GetStoredEpisodeModel(programId, fileUrl, episode);
                        var transferBlockBlob = await _storageTransfer.TransferBlockBlobIfNotExists(_cloudBlobContainerName, storedEpisode.AudioBlobIdentifier, storedEpisode.OriginalAudioUrl);
                        storedEpisode.AudioUrl = transferBlockBlob.ToString();

                        storedEpisodes.Add(storedEpisode);

                        await _storage.StoreEpisode(episode.Program.Id, episode.Id, storedEpisode);

                        _logger.LogInformation($"Collected SR episode {episode.Id}");
                    });

                    tasks.Add(task);
                }
            }

            await Task.WhenAll(tasks);

            return storedEpisodes.ToList();
        }

        private async Task<SrStoredEpisode> GetStoredEpisodeModel(int programId, string fileUrl, Episode episode)
        {
            var finalUri = await GetUriAfterOneRedirect(fileUrl);
            if (finalUri.Host == "")
            {
                finalUri = new Uri((new Uri(fileUrl)).Host + finalUri);
            }

            var finalUrl = finalUri.ToString();
            var extension = finalUrl.Substring(finalUrl.LastIndexOf('.') + 1);

            var name = GetBlobName(programId, episode, extension);
            return new SrStoredEpisode
            {
                Episode = episode,
                OriginalAudioUrl = finalUrl,

                AudioBlobIdentifier = name,
                AudioExtension = extension,
                AudioLocale = SrEpisodeMetadata.GetAudioLocaleForEpisode(episode)
            };
        }

        private static string GetBlobName(int programId, Episode episode, string extension)
        {
            return $"SR/{programId}/SR_{programId}__{episode.PublishDateUtc:yyyy-MM-dd}_{episode.PublishDateUtc:HH-mm}__{episode.Id}.{extension}";
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
                || httpResult.StatusCode == HttpStatusCode.Moved
                || httpResult.StatusCode == HttpStatusCode.TemporaryRedirect)
            {
                return httpResult.Headers.Location;
            }

            return new Uri(url);
        }
    }
}

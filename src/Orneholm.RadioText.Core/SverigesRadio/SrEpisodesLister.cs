using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Orneholm.SverigesRadio.Api;
using Orneholm.SverigesRadio.Api.Models.Request;
using Orneholm.SverigesRadio.Api.Models.Response.Episodes;

namespace Orneholm.RadioText.Core.SverigesRadio
{
    public class SrEpisodesLister
    {
        private readonly ISverigesRadioApiClient _sverigesRadioApiClient;
        private readonly ILogger<SrEpisodeCollector> _logger;

        public SrEpisodesLister(ISverigesRadioApiClient sverigesRadioApiClient, ILogger<SrEpisodeCollector> logger)
        {
            _sverigesRadioApiClient = sverigesRadioApiClient;
            _logger = logger;
        }


        public async Task<List<Episode>> List(List<int> programIds, int count)
        {
            var episodes = new ConcurrentBag<Episode>();
            var tasks = new List<Task>();

            foreach (var programId in programIds)
            {
                _logger.LogInformation($"Listing episodes for program {programId}");
                tasks.Add(List(programId, count).ContinueWith(x =>
                {
                    foreach (var episode in x.Result)
                    {
                        episodes.Add(episode);
                    }
                    _logger.LogInformation($"Listed {x.Result.Count} episodes for program {programId}");
                }));
            }

            await Task.WhenAll(tasks);

            return episodes.ToList();
        }

        public async Task<List<Episode>> List(int programId, int count)
        {
            var episodesResult = await _sverigesRadioApiClient.ListEpisodesAsync(programId, pagination: ListPagination.TakeFirst(count));

            return episodesResult.Episodes;
        }
    }
}

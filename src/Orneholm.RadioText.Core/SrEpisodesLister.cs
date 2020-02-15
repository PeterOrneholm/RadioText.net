using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Orneholm.SverigesRadio.Api;
using Orneholm.SverigesRadio.Api.Models.Request;
using Orneholm.SverigesRadio.Api.Models.Response.Episodes;

namespace Orneholm.RadioText.Core
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


        public async Task<List<Episode>> List(Dictionary<int, int> programIdsAndCount)
        {
            var episodes = new ConcurrentBag<Episode>();
            var tasks = new List<Task>();

            foreach (var program in programIdsAndCount)
            {
                _logger.LogInformation($"Listing episodes for program {program.Key}");
                tasks.Add(List(program.Key, program.Value).ContinueWith(x =>
                {
                    foreach (var episode in x.Result)
                    {
                        episodes.Add(episode);
                    }
                    _logger.LogInformation($"Listed {x.Result.Count} episodes for program {program.Key}");
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

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Orneholm.SverigesRadio.Api;
using Orneholm.SverigesRadio.Api.Models.Request;
using Orneholm.SverigesRadio.Api.Models.Request.Episodes;
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

        public async Task<List<Episode>> List(Dictionary<int, DateRange> programIdsAndCount)
        {
            var episodes = new ConcurrentBag<Episode>();
            var tasks = new List<Task>();

            foreach (var program in programIdsAndCount)
            {
                _logger.LogInformation($"Listing episodes for program {program.Key} for date range {program.Value.FromDate} to {program.Value.ToDate}");
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

        public async Task<List<Episode>> List(int programId, DateRange dateRange)
        {
            var episodes = new List<Episode>();

            var request = new EpisodeListRequest(programId) { FromDate = dateRange.FromDate, ToDate = dateRange.ToDate };
            await foreach (var episode in _sverigesRadioApiClient.ListAllEpisodesAsync(request))
            {
                episodes.Add(episode);
            }

            return episodes;
        }
    }

    public class DateRange
    {
        public DateRange(DateTime fromDate) : this(fromDate, null)
        {
            FromDate = fromDate;
        }

        public DateRange(DateTime? fromDate, DateTime? toDate)
        {
            FromDate = fromDate;
            ToDate = toDate;
        }

        public DateTime? FromDate { get; }
        public DateTime? ToDate { get; }
    }
}

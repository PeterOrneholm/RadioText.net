using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Orneholm.RadioText.Core.Storage;
using Orneholm.RadioText.Core.SverigesRadio;
using Orneholm.SverigesRadio.Api;

namespace Orneholm.RadioText.Worker
{
    public class Worker : BackgroundService
    {
        private readonly ILogger<Worker> _logger;
        private readonly SrEpisodeCollector _srEpisodeCollector;

        private readonly int[] _srProgramIds =
        {
            SverigesRadioApiIds.Programs.Ekot,
            SverigesRadioApiIds.Programs.RadioSweden
        };

        private readonly int _srProgramIdCount = 20;

        public Worker(ILogger<Worker> logger, SrEpisodeCollector srEpisodeCollector)
        {
            _logger = logger;
            _srEpisodeCollector = srEpisodeCollector;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                var collectedEpisodes = await CollectEpisodes();

                foreach (var collectedEpisode in collectedEpisodes)
                {
                    _logger.LogInformation($"Collected: {collectedEpisode.Episode?.Title}");
                }

                await Task.Delay(1000, stoppingToken);
            }
        }

        private async Task<List<SrStoredEpisode>> CollectEpisodes()
        {
            return await _srEpisodeCollector.Collect(_srProgramIds.ToList(), _srProgramIdCount);
        }
    }
}

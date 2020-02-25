using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Orneholm.RadioText.Core;

namespace Orneholm.RadioText.Worker
{
    public class Worker : BackgroundService
    {
        private static readonly Dictionary<int, int> SrPrograms = new Dictionary<int, int>
        {
            { SrProgramIds.Ekot, 2 },

            { SrProgramIds.RadioSweden_English, 3 },
            { SrProgramIds.RadioSweden_Arabic, 2 },
            { SrProgramIds.RadioSweden_Finnish, 2 },

            { SrProgramIds.P3Nyheter, 2 },

            { SrProgramIds.P4_Stockholm, 5 },
        };

        private readonly ILogger<Worker> _logger;
        private readonly SrWorker _srWorker;

        public Worker(ILogger<Worker> logger, SrWorker srWorker)
        {
            _logger = logger;
            _srWorker = srWorker;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                await _srWorker.Work(SrPrograms, true, stoppingToken);
                await Task.Delay(TimeSpan.FromMinutes(15), stoppingToken);
            }
        }
    }
}

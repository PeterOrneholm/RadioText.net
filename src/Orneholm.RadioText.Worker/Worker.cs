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
            { SrProgramIds.Ekot, 1500 },

            //{ SrProgramIds.RadioSweden_English, 5 },

            //{ SrProgramIds.P4_Stockholm, 20 }
        };


        private static readonly Dictionary<int, DateRange> SrProgramWithDates = new Dictionary<int, DateRange>
        {
            { SrProgramIds.Ekot, new DateRange(DateTime.Now.AddDays(-7)) }
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
                await _srWorker.Work(SrProgramWithDates, true, stoppingToken);
                await Task.Delay(TimeSpan.FromMinutes(15), stoppingToken);
            }
        }
    }
}

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Azure.WebJobs;
using Microsoft.Extensions.Logging;
using Orneholm.RadioText.Core;

namespace Orneholm.RadioText.AzureFunctions
{
    public class SrEpisodesFunctions
    {
        private static readonly Dictionary<int, int> SrPrograms = new Dictionary<int, int>
        {
            { SrProgramIds.Ekot, 5 },

            { SrProgramIds.RadioSweden_English, 5 },

            { SrProgramIds.P4_Stockholm, 5 }
        };

        private readonly SrWorker _srWorker;

        public SrEpisodesFunctions(SrWorker srWorker)
        {
            _srWorker = srWorker;
        }

        [FunctionName("CrawlSrEpisodes")]
        public async Task Run([TimerTrigger("0 * * * *", RunOnStartup = true)]TimerInfo myTimer, ILogger log, CancellationToken cancellationToken)
        {
            log.LogInformation($"C# Timer trigger function executed at: {DateTime.Now}");
            await _srWorker.Work(SrPrograms, false, cancellationToken);
        }
    }
}

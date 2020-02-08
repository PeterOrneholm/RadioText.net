using Microsoft.Azure.Storage;
using Microsoft.Azure.Storage.Blob;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Orneholm.RadioText.Core.Storage;
using Orneholm.RadioText.Core.SverigesRadio;
using Orneholm.SverigesRadio.Api;
using Orneholm.SverigesRadio.Api.Models.Request.Common;

namespace Orneholm.RadioText.Worker
{
    public class Program
    {
        public static void Main(string[] args)
        {
            CreateHostBuilder(args).Build().Run();
        }

        public static IHostBuilder CreateHostBuilder(string[] args) =>
            Host.CreateDefaultBuilder(args)
                .ConfigureServices((hostContext, services) =>
                {
                    services.AddTransient<ISverigesRadioApiClient>(s => SverigesRadioApiClient.CreateClient(new AudioSettings
                    {
                        AudioQuality = AudioQuality.High,
                        OnDemandAudioTemplateId = SverigesRadioApiIds.OnDemandAudioTemplates.M4A_M3U8
                    }));

                    services.AddTransient(x =>
                    {
                        var storageAccount = CloudStorageAccount.Parse(hostContext.Configuration["AzureStorage:ConnectionString"]);
                        return storageAccount.CreateCloudBlobClient();
                    });

                    services.AddTransient<IStorageTransfer, AzureStorageTransfer>();
                    services.AddTransient<IStorage, InMemoryStorage>();
                    services.AddTransient<SrEpisodeCollector>(s => new SrEpisodeCollector(hostContext.Configuration["AzureStorage:AudioContainerName"],
                        s.GetRequiredService<IStorageTransfer>(),
                        s.GetRequiredService<ISverigesRadioApiClient>(),
                        s.GetRequiredService<ILogger<SrEpisodeCollector>>(),
                        s.GetRequiredService<IStorage>()
                    ));

                    services.AddHostedService<Worker>();
                });
    }
}

using Microsoft.Azure.Cosmos.Table;
using Microsoft.Azure.Storage.Blob;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Orneholm.RadioText.Azure.SpeechBatchClient;
using Orneholm.RadioText.Core;
using Orneholm.RadioText.Core.Storage;
using Orneholm.SverigesRadio.Api;
using CloudStorageAccount = Microsoft.Azure.Storage.CloudStorageAccount;

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
                    services.AddTransient<ISverigesRadioApiClient>(s => SverigesRadioApiClient.CreateClient());

                    services.AddTransient(x =>
                    {
                        var storageAccount = CloudStorageAccount.Parse(hostContext.Configuration["AzureStorage:ConnectionString"]);
                        return storageAccount.CreateCloudBlobClient();
                    });

                    services.AddTransient(x =>
                    {
                        var storageAccount = Microsoft.Azure.Cosmos.Table.CloudStorageAccount.Parse(hostContext.Configuration["AzureStorage:ConnectionString"]);
                        return storageAccount.CreateCloudTableClient(new TableClientConfiguration());
                    });

                    services.AddTransient(x => SpeechBatchClient.CreateApiV2Client(hostContext.Configuration["AzureSpeech:Key"], hostContext.Configuration["AzureSpeech:Hostname"], 443));

                    services.AddTransient<IStorageTransfer, AzureStorageTransfer>();
                    services.AddTransient<IStorage, AzureTableStorage>(s => new AzureTableStorage(
                        s.GetRequiredService<CloudTableClient>(),
                        hostContext.Configuration["AzureStorage:EpisodesTableName"],
                        hostContext.Configuration["AzureStorage:EpisodeTranscriptionsTableName"]
                    ));

                    services.AddTransient<SrEpisodesLister>();
                    services.AddTransient(s => new SrEpisodeCollector(
                        hostContext.Configuration["AzureStorage:AudioContainerName"],
                        s.GetRequiredService<IStorageTransfer>(),
                        s.GetRequiredService<ISverigesRadioApiClient>(),
                        s.GetRequiredService<ILogger<SrEpisodeCollector>>(),
                        s.GetRequiredService<IStorage>()
                    ));

                    services.AddTransient(s => new SrEpisodeTranscriber(
                        hostContext.Configuration["AzureStorage:EpisodeTranscriptionsContainerName"],
                        s.GetRequiredService<SpeechBatchClient>(),
                        s.GetRequiredService<IStorageTransfer>(),
                        s.GetRequiredService<ILogger<SrEpisodeCollector>>(),
                        s.GetRequiredService<IStorage>(),
                        s.GetRequiredService<CloudBlobClient>()
                    ));

                    services.AddHostedService<Worker>();
                });
    }
}

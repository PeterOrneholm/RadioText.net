using Microsoft.Azure.Cosmos.Table;
using Microsoft.Azure.Storage.Blob;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Orneholm.RadioText.Core.Storage;
using Orneholm.RadioText.Core.SverigesRadio;
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

                    services.AddTransient<IStorageTransfer, AzureStorageTransfer>();
                    //services.AddTransient<IStorage, InMemoryStorage>();
                    services.AddTransient<IStorage, AzureTableStorage>(s =>
                    {
                        return new AzureTableStorage(s.GetRequiredService<CloudTableClient>(), hostContext.Configuration["AzureStorage:EpisodesTableName"]);
                    });
                    services.AddTransient<SrEpisodesLister>();
                    services.AddTransient(s => new SrEpisodeCollector(hostContext.Configuration["AzureStorage:AudioContainerName"],
                        s.GetRequiredService<IStorageTransfer>(),
                        s.GetRequiredService<ISverigesRadioApiClient>(),
                        s.GetRequiredService<ILogger<SrEpisodeCollector>>(),
                        s.GetRequiredService<IStorage>()
                    ));

                    services.AddHostedService<Worker>();
                });
    }
}

using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Azure.CognitiveServices.Language.TextAnalytics;
using Microsoft.Azure.Cosmos.Table;
using Microsoft.Azure.Storage.Blob;
using Microsoft.CognitiveServices.Speech;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Rest;
using Orneholm.RadioText.Azure.TranslatorClient;
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
                    ServicePointManager.DefaultConnectionLimit = 100;

                    services.AddSingleton<ISverigesRadioApiClient>(s => SverigesRadioApiClient.CreateClient());

                    services.AddTransient(x =>
                    {
                        var storageAccount = CloudStorageAccount.Parse(hostContext.Configuration["AzureStorage:BlobsConnectionString"]);
                        return storageAccount.CreateCloudBlobClient();
                    });

                    services.AddTransient(x =>
                    {
                        var storageAccount = Microsoft.Azure.Cosmos.Table.CloudStorageAccount.Parse(hostContext.Configuration["AzureStorage:TablesConnectionString"]);
                        return storageAccount.CreateCloudTableClient(new TableClientConfiguration());
                    });


                    var azureSpeechClients = new List<SpeechBatchClientOptions>();
                    hostContext.Configuration.GetSection("AzureSpeech:Clients").Bind(azureSpeechClients);
                    services.AddSingleton<ISpeechConfigFactory>(x => new RoundRobinSpeechConfigFactory(azureSpeechClients));
                    services.AddSingleton<ISpeechBatchClientFactory>(x => new RoundRobinSpeechBatchClientFactory(azureSpeechClients));

                    services.AddTransient(x =>
                    {
                        var credentials = new ApiKeyServiceClientCredentials(hostContext.Configuration["AzureTextAnalytics:Key"]);
                        return new TextAnalyticsClient(credentials)
                        {
                            Endpoint = hostContext.Configuration["AzureTextAnalytics:Endpoint"]
                        };
                    });

                    services.AddTransient(x => TranslatorClient.CreateClient(hostContext.Configuration["AzureTranslator:Key"], hostContext.Configuration["AzureTranslator:Endpoint"]));

                    services.AddTransient<IStorageTransfer, AzureStorageTransfer>();
                    services.AddTransient<IStorage, AzureTableStorage>(s => new AzureTableStorage(
                        s.GetRequiredService<CloudTableClient>(),
                        hostContext.Configuration["AzureStorage:EpisodeStatusesTableName"],
                        hostContext.Configuration["AzureStorage:EpisodesTableName"],
                        hostContext.Configuration["AzureStorage:EpisodeTranscriptionsTableName"],
                        hostContext.Configuration["AzureStorage:EpisodeTextAnalyticsTableName"],
                        hostContext.Configuration["AzureStorage:EpisodeSpeechTableName"]
                    ));

                    services.AddTransient<ISummaryStorage, SummaryAzureTableStorage>(s => new SummaryAzureTableStorage(
                        s.GetRequiredService<CloudTableClient>(),
                        hostContext.Configuration["AzureStorage:EpisodeSummaryTableName"]
                    ));


                    services.AddTransient<IWordCountStorage, WordCounterAzureTableStorage>(s =>
                    {
                        var storageAccount = Microsoft.Azure.Cosmos.Table.CloudStorageAccount.Parse(hostContext.Configuration["AzureStorage:BlobsConnectionString"]);
                        var tableClient = storageAccount.CreateCloudTableClient(new TableClientConfiguration());

                        return new WordCounterAzureTableStorage(
                            tableClient,
                            hostContext.Configuration["AzureStorage:EpisodeWordCountTableName"]
                        );
                    });

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
                        s.GetRequiredService<ISpeechBatchClientFactory>(),
                        s.GetRequiredService<IStorageTransfer>(),
                        s.GetRequiredService<ILogger<SrEpisodeCollector>>(),
                        s.GetRequiredService<IStorage>(),
                        s.GetRequiredService<CloudBlobClient>()
                    ));

                    services.AddTransient<SrEpisodeTextEnricher>();
                    services.AddTransient<SrEpisodeSummarizer>();
                    services.AddTransient(s => new SrEpisodeSpeaker(
                        hostContext.Configuration["AzureStorage:EpisodeSpeechContainerName"],
                        s.GetRequiredService<ISpeechConfigFactory>(),
                        s.GetRequiredService<IStorage>(),
                        s.GetRequiredService<ILogger<SrEpisodeSpeaker>>(),
                        s.GetRequiredService<CloudBlobClient>()
                    ));

                    services.AddTransient<SrEpisodeWordCounter>();

                    services.AddTransient<SrWorker>();

                    services.AddHostedService<Worker>();
                });


        private class ApiKeyServiceClientCredentials : ServiceClientCredentials
        {
            private readonly string _apiKey;

            public ApiKeyServiceClientCredentials(string apiKey)
            {
                this._apiKey = apiKey;
            }

            public override Task ProcessHttpRequestAsync(HttpRequestMessage request, CancellationToken cancellationToken)
            {
                if (request == null)
                {
                    throw new ArgumentNullException("request");
                }
                request.Headers.Add("Ocp-Apim-Subscription-Key", this._apiKey);
                return base.ProcessHttpRequestAsync(request, cancellationToken);
            }
        }
    }
}

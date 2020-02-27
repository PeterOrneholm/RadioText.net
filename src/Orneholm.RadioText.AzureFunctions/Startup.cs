using System;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Azure.CognitiveServices.Language.TextAnalytics;
using Microsoft.Azure.Cosmos.Table;
using Microsoft.Azure.Functions.Extensions.DependencyInjection;
using Microsoft.Azure.Storage.Blob;
using Microsoft.CognitiveServices.Speech;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Rest;
using Orneholm.RadioText.Azure.SpeechBatchClient;
using Orneholm.RadioText.Azure.TranslatorClient;
using Orneholm.RadioText.AzureFunctions;
using Orneholm.RadioText.Core;
using Orneholm.RadioText.Core.Storage;
using Orneholm.SverigesRadio.Api;
using CloudStorageAccount = Microsoft.Azure.Storage.CloudStorageAccount;

[assembly: FunctionsStartup(typeof(Startup))]

namespace Orneholm.RadioText.AzureFunctions
{
    public class Startup : FunctionsStartup
    {
        public override void Configure(IFunctionsHostBuilder builder)
        {
            var services = builder.Services;
            
            services.AddTransient<ISverigesRadioApiClient>(s => SverigesRadioApiClient.CreateClient());

            services.AddTransient(x =>
            {
                var configuration = x.GetRequiredService<IConfiguration>();
                var storageAccount = CloudStorageAccount.Parse(configuration["AzureStorage:BlobsConnectionString"]);
                return storageAccount.CreateCloudBlobClient();
            });

            services.AddTransient(x =>
            {
                var configuration = x.GetRequiredService<IConfiguration>();
                var storageAccount = Microsoft.Azure.Cosmos.Table.CloudStorageAccount.Parse(configuration["AzureStorage:TablesConnectionString"]);
                return storageAccount.CreateCloudTableClient(new TableClientConfiguration());
            });

            services.AddTransient(x =>
            {
                var configuration = x.GetRequiredService<IConfiguration>();
                return SpeechConfig.FromSubscription(configuration["AzureSpeech:Key"], configuration["AzureSpeech:Region"]);
            });
            services.AddTransient(x =>
            {
                var configuration = x.GetRequiredService<IConfiguration>();
                return SpeechBatchClient.CreateApiV2Client(configuration["AzureSpeech:Key"], configuration["AzureSpeech:Hostname"], 443);
            });

            services.AddTransient(x =>
            {
                var configuration = x.GetRequiredService<IConfiguration>();
                var credentials = new ApiKeyServiceClientCredentials(configuration["AzureTextAnalytics:Key"]);
                return new TextAnalyticsClient(credentials)
                {
                    Endpoint = configuration["AzureTextAnalytics:Endpoint"]
                };
            });

            services.AddTransient(x =>
            {
                var configuration = x.GetRequiredService<IConfiguration>();
                return TranslatorClient.CreateClient(configuration["AzureTranslator:Key"], configuration["AzureTranslator:Endpoint"]);
            });

            services.AddTransient<IStorageTransfer, AzureStorageTransfer>();
            services.AddTransient<IStorage, AzureTableStorage>(s =>
            {
                var configuration = s.GetRequiredService<IConfiguration>();
                return new AzureTableStorage(
                    s.GetRequiredService<CloudTableClient>(),
                    configuration["AzureStorage:EpisodeStatusesTableName"],
                    configuration["AzureStorage:EpisodesTableName"],
                    configuration["AzureStorage:EpisodeTranscriptionsTableName"],
                    configuration["AzureStorage:EpisodeTextAnalyticsTableName"],
                    configuration["AzureStorage:EpisodeSpeechTableName"]
                );
            });

            services.AddTransient<ISummaryStorage, SummaryAzureTableStorage>(s =>
            {
                var configuration = s.GetRequiredService<IConfiguration>();
                return new SummaryAzureTableStorage(
                    s.GetRequiredService<CloudTableClient>(),
                    configuration["AzureStorage:EpisodeSummaryTableName"]
                );
            });

            services.AddTransient<SrEpisodesLister>();
            services.AddTransient(s =>
            {
                var configuration = s.GetRequiredService<IConfiguration>();
                return new SrEpisodeCollector(
                    configuration["AzureStorage:AudioContainerName"],
                    s.GetRequiredService<IStorageTransfer>(),
                    s.GetRequiredService<ISverigesRadioApiClient>(),
                    s.GetRequiredService<ILogger<SrEpisodeCollector>>(),
                    s.GetRequiredService<IStorage>()
                );
            });

            services.AddTransient(s =>
            {
                var configuration = s.GetRequiredService<IConfiguration>();
                return new SrEpisodeTranscriber(
                    configuration["AzureStorage:EpisodeTranscriptionsContainerName"],
                    s.GetRequiredService<SpeechBatchClient>(),
                    s.GetRequiredService<IStorageTransfer>(),
                    s.GetRequiredService<ILogger<SrEpisodeCollector>>(),
                    s.GetRequiredService<IStorage>(),
                    s.GetRequiredService<CloudBlobClient>()
                );
            });

            services.AddTransient<SrEpisodeTextEnricher>();
            services.AddTransient<SrEpisodeSummarizer>();
            services.AddTransient(s =>
            {
                var configuration = s.GetRequiredService<IConfiguration>();
                return new SrEpisodeSpeaker(
                    configuration["AzureStorage:EpisodeSpeechContainerName"],
                    s.GetRequiredService<SpeechConfig>(),
                    s.GetRequiredService<IStorage>(),
                    s.GetRequiredService<ILogger<SrEpisodeSpeaker>>(),
                    s.GetRequiredService<CloudBlobClient>()
                );
            });

            services.AddTransient<SrWorker>();
        }

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

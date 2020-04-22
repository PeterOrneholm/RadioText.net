using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Azure.Cosmos.Table;

namespace Orneholm.RadioText.Core.Storage
{
    public class WordCounterAzureTableStorage : IWordCountStorage
    {
        private readonly CloudTable _episodeWordCountTable;

        public WordCounterAzureTableStorage(CloudTableClient cloudTableClient, string episodeWordCountTableName)
        {
            _episodeWordCountTable = cloudTableClient.GetTableReference(episodeWordCountTableName);
        }

        public async Task StoreWordCounterEpisode(int episodeId, SrStoredWordCountEpisode episode)
        {
            await _episodeWordCountTable.CreateIfNotExistsAsync();

            var entity = new SrStoredWordCountEpisodeEntity(episodeId, episode);
            var insertOrMergeOperation = TableOperation.InsertOrMerge(entity);
            await _episodeWordCountTable.ExecuteAsync(insertOrMergeOperation);
        }

        public class SrStoredWordCountEpisodeEntity : TableEntity
        {
            public SrStoredWordCountEpisodeEntity()
            {
            }

            public SrStoredWordCountEpisodeEntity(int episodeId, SrStoredWordCountEpisode episode)
            {
                PartitionKey = "SrStoredWordCountEpisode";
                RowKey = episodeId.ToString("D");

                EpisodeId = episodeId;

                EpisodeAudioUrl = episode.EpisodeAudioUrl;
                EpisodeAudioLocale = episode.EpisodeAudioLocale;

                EpisodeTitle = episode.EpisodeTitle;
                EpisodeUrl = episode.EpisodeUrl;

                EpisodePublishDateUtc = episode.EpisodePublishDateUtc;

                ProgramId = episode.ProgramId;
                ProgramName = episode.ProgramName;

                EpisodeAudioTranscription = episode.EpisodeAudioTranscription;

                WordCount = episode.WordCount;
            }

            public override IDictionary<string, EntityProperty> WriteEntity(OperationContext operationContext)
            {
                var dictionary = base.WriteEntity(operationContext);

                foreach (var wordCount in WordCount)
                {
                    var key = "WordCount_" + wordCount.Key.Replace("å", "a").Replace("ä", "a").Replace("ö", "o").Replace(" ", "_");
                    dictionary.Add(key, new EntityProperty(wordCount.Value));
                }

                return dictionary;
            }

            public override void ReadEntity(IDictionary<string, EntityProperty> properties, OperationContext operationContext)
            {
                base.ReadEntity(properties, operationContext);

                foreach (var entityProperty in properties)
                {
                    if (entityProperty.Key.StartsWith("WordCount_"))
                    {
                        WordCount.Add(entityProperty.Key.Substring(10), entityProperty.Value.Int32Value ?? 0);
                    }
                }
            }
            public int EpisodeId { get; set; }

            public string EpisodeAudioUrl { get; set; } = string.Empty;
            public string EpisodeAudioLocale { get; set; } = string.Empty;

            public string EpisodeTitle { get; set; } = string.Empty;
            public string EpisodeUrl { get; set; } = string.Empty;
            public DateTime EpisodePublishDateUtc { get; set; }

            public int ProgramId { get; set; }
            public string ProgramName { get; set; } = string.Empty;

            public string EpisodeAudioTranscription { get; set; } = string.Empty;

            [IgnoreProperty]
            public Dictionary<string, int> WordCount { get; set; } = new Dictionary<string, int>();
        }
    }
}

using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.Azure.Cosmos.Table;
using Orneholm.SverigesRadio.Api.Models.Response.Episodes;

namespace Orneholm.RadioText.Core.Storage
{
    public class AzureTableStorage : IStorage
    {
        private readonly CloudTableClient _cloudTableClient;
        private readonly CloudTable _episodesTable;

        public AzureTableStorage(CloudTableClient cloudTableClient, string episodesTableName)
        {
            _cloudTableClient = cloudTableClient;
            _episodesTable = _cloudTableClient.GetTableReference(episodesTableName);
        }

        public async Task<SrStoredEpisode?> GetEpisode(int episodeId)
        {
            await _episodesTable.CreateIfNotExistsAsync();

            var retrieveOperation = TableOperation.Retrieve<SrStoredEpisodeEntity>("SrStoredEpisode", episodeId.ToString("D"));
            var result = await _episodesTable.ExecuteAsync(retrieveOperation);
            var srStoredEpisodeEntity = result.Result as SrStoredEpisodeEntity;

            if (srStoredEpisodeEntity == null)
            {
                return null;
            }

            return new SrStoredEpisode
            {
                Episode = srStoredEpisodeEntity.Episode,

                OriginalAudioUrl = srStoredEpisodeEntity.OriginalAudioUrl,
                AudioBlobIdentifier = srStoredEpisodeEntity.AudioBlobIdentifier,
                AudioUrl = srStoredEpisodeEntity.AudioUrl,
                AudioExtension = srStoredEpisodeEntity.AudioExtension,
                AudioLocale = srStoredEpisodeEntity.AudioLocale
            };
        }

        public async Task StoreEpisode(int episodeId, SrStoredEpisode episode)
        {
            await _episodesTable.CreateIfNotExistsAsync();

            var entity = new SrStoredEpisodeEntity(episodeId, episode);
            var insertOrMergeOperation = TableOperation.InsertOrMerge(entity);
            await _episodesTable.ExecuteAsync(insertOrMergeOperation);
        }


        public class SrStoredEpisodeEntity : TableEntity
        {
            public SrStoredEpisodeEntity()
            {
            }

            public SrStoredEpisodeEntity(int episodeId, SrStoredEpisode episode)
            {
                PartitionKey = "SrStoredEpisode";
                RowKey = episodeId.ToString("D");

                Episode = episode.Episode;

                EpisodeId = episode.Episode.Id;
                EpisodeTitle = episode.Episode.Title;

                OriginalAudioUrl = episode.OriginalAudioUrl;
                AudioBlobIdentifier = episode.AudioBlobIdentifier;
                AudioUrl = episode.AudioUrl;
                AudioExtension = episode.AudioExtension;
                AudioLocale = episode.AudioLocale;
            }

            [IgnoreProperty]
            public Episode Episode
            {
                get => JsonSerializer.Deserialize<Episode>(EpisodeJson);
                set => EpisodeJson = JsonSerializer.Serialize(value);
            }

            public string EpisodeJson { get; set; } = string.Empty;

            public int EpisodeId { get; set; }
            public string EpisodeTitle { get; set; } = string.Empty;

            public string OriginalAudioUrl { get; set; } = string.Empty;
            public string AudioBlobIdentifier { get; set; } = string.Empty;
            public string AudioUrl { get; set; } = string.Empty;
            public string AudioExtension { get; set; } = string.Empty;
            public string AudioLocale { get; set; } = string.Empty;
        }
    }
}
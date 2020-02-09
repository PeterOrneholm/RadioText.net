using System;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.Azure.Cosmos.Table;
using Orneholm.SverigesRadio.Api.Models.Response.Episodes;

namespace Orneholm.RadioText.Core.Storage
{
    public class AzureTableStorage : IStorage
    {
        private readonly CloudTable _episodesTable;
        private readonly CloudTable _episodeTranscriptionsTable;

        public AzureTableStorage(CloudTableClient cloudTableClient, string episodesTableName, string episodeTranscriptionsTableName)
        {
            _episodesTable = cloudTableClient.GetTableReference(episodesTableName);
            _episodeTranscriptionsTable = cloudTableClient.GetTableReference(episodeTranscriptionsTableName);
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

        public async Task<SrStoredEpisodeTranscription?> GetEpisodeTranscription(int episodeId)
        {
            await _episodeTranscriptionsTable.CreateIfNotExistsAsync();

            var retrieveOperation = TableOperation.Retrieve<SrStoredEpisodeTranscriptionEntity>("SrStoredEpisodeTranscription", episodeId.ToString("D"));
            var result = await _episodeTranscriptionsTable.ExecuteAsync(retrieveOperation);
            var srStoredEpisodeTranscriptionEntity = result.Result as SrStoredEpisodeTranscriptionEntity;

            if (srStoredEpisodeTranscriptionEntity == null)
            {
                return null;
            }

            return new SrStoredEpisodeTranscription
            {
                EpisodeId = srStoredEpisodeTranscriptionEntity.EpisodeId,

                Status = srStoredEpisodeTranscriptionEntity.Status,

                TranscriptionResultChannel0BlobIdentifier = srStoredEpisodeTranscriptionEntity.TranscriptionResultChannel0BlobIdentifier,
                TranscriptionResultChannel0Url = srStoredEpisodeTranscriptionEntity.TranscriptionResultChannel0Url,

                TranscriptionResultChannel1BlobIdentifier = srStoredEpisodeTranscriptionEntity.TranscriptionResultChannel1BlobIdentifier,
                TranscriptionResultChannel1Url = srStoredEpisodeTranscriptionEntity.TranscriptionResultChannel1Url,

                CombinedDisplayResult = srStoredEpisodeTranscriptionEntity.CombinedDisplayResult
            };
        }

        public async Task StoreTranscription(int episodeId, SrStoredEpisodeTranscription episodeTranscription)
        {
            await _episodeTranscriptionsTable.CreateIfNotExistsAsync();

            var entity = new SrStoredEpisodeTranscriptionEntity(episodeId, episodeTranscription);
            var insertOrMergeOperation = TableOperation.InsertOrMerge(entity);
            await _episodeTranscriptionsTable.ExecuteAsync(insertOrMergeOperation);
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

                EpisodeId = episodeId;
                EpisodeTitle = episode.Episode.Title;
                EpisodePublishDateUtc = episode.Episode.PublishDateUtc;

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
            public DateTime EpisodePublishDateUtc { get; set; }

            public string OriginalAudioUrl { get; set; } = string.Empty;
            public string AudioBlobIdentifier { get; set; } = string.Empty;
            public string AudioUrl { get; set; } = string.Empty;
            public string AudioExtension { get; set; } = string.Empty;
            public string AudioLocale { get; set; } = string.Empty;
        }

        public class SrStoredEpisodeTranscriptionEntity : TableEntity
        {
            public SrStoredEpisodeTranscriptionEntity()
            {
            }

            public SrStoredEpisodeTranscriptionEntity(int episodeId, SrStoredEpisodeTranscription episodeTranscription)
            {
                PartitionKey = "SrStoredEpisodeTranscription";
                RowKey = episodeId.ToString("D");

                EpisodeId = episodeId;

                Status = episodeTranscription.Status;

                TranscriptionResultChannel0BlobIdentifier = episodeTranscription.TranscriptionResultChannel0BlobIdentifier;
                TranscriptionResultChannel0Url = episodeTranscription.TranscriptionResultChannel0Url;

                TranscriptionResultChannel1BlobIdentifier = episodeTranscription.TranscriptionResultChannel1BlobIdentifier;
                TranscriptionResultChannel1Url = episodeTranscription.TranscriptionResultChannel1Url;

                CombinedDisplayResult = episodeTranscription.CombinedDisplayResult;
            }

            public int EpisodeId { get; set; }

            public string Status { get; set; } = string.Empty;

            public string TranscriptionResultChannel0BlobIdentifier { get; set; } = string.Empty;
            public string TranscriptionResultChannel0Url { get; set; } = string.Empty;

            public string TranscriptionResultChannel1BlobIdentifier { get; set; } = string.Empty;
            public string TranscriptionResultChannel1Url { get; set; } = string.Empty;

            public string CombinedDisplayResult { get; set; } = string.Empty;
        }
    }
}

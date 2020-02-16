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
        private readonly CloudTable _episodeEnrichedTable;
        private readonly CloudTable _episodeSummarizedTable;
        private readonly CloudTable _episodeSpeechTable;

        public AzureTableStorage(CloudTableClient cloudTableClient, string episodesTableName, string episodeTranscriptionsTableName, string episodeEnrichedTableName, string episodeSummarizedTableName, string episodeSpeechTableName)
        {
            _episodesTable = cloudTableClient.GetTableReference(episodesTableName);
            _episodeTranscriptionsTable = cloudTableClient.GetTableReference(episodeTranscriptionsTableName);
            _episodeEnrichedTable = cloudTableClient.GetTableReference(episodeEnrichedTableName);
            _episodeSummarizedTable = cloudTableClient.GetTableReference(episodeSummarizedTableName);
            _episodeSpeechTable = cloudTableClient.GetTableReference(episodeSpeechTableName);
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

        public async Task<SrStoredEnrichedEpisode?> GetEnrichedEpisode(int episodeId)
        {
            await _episodeEnrichedTable.CreateIfNotExistsAsync();

            var retrieveOperation = TableOperation.Retrieve<SrStoredEnrichedEpisodeEntity>("SrStoredEnrichedEpisode", episodeId.ToString("D"));
            var result = await _episodeEnrichedTable.ExecuteAsync(retrieveOperation);
            var srStoredEnrichedEpisodeEntity = result.Result as SrStoredEnrichedEpisodeEntity;

            if (srStoredEnrichedEpisodeEntity == null)
            {
                return null;
            }

            return new SrStoredEnrichedEpisode
            {
                EpisodeId = srStoredEnrichedEpisodeEntity.EpisodeId,

                Title_Original = srStoredEnrichedEpisodeEntity.Title_Original,
                Description_Original = srStoredEnrichedEpisodeEntity.Description_Original,
                Transcription_Original = srStoredEnrichedEpisodeEntity.Transcription_Original,

                Title_EN = srStoredEnrichedEpisodeEntity.Title_EN,
                Description_EN = srStoredEnrichedEpisodeEntity.Description_EN,
                Transcription_EN = srStoredEnrichedEpisodeEntity.Transcription_EN,

                Title_SV = srStoredEnrichedEpisodeEntity.Title_SV,
                Description_SV = srStoredEnrichedEpisodeEntity.Description_SV,
                Transcription_SV = srStoredEnrichedEpisodeEntity.Transcription_SV,
            };
        }

        public async Task StoreEnrichedEpisode(int episodeId, SrStoredEnrichedEpisode episode)
        {
            await _episodeEnrichedTable.CreateIfNotExistsAsync();

            var entity = new SrStoredEnrichedEpisodeEntity(episodeId, episode);
            var insertOrMergeOperation = TableOperation.InsertOrMerge(entity);
            await _episodeEnrichedTable.ExecuteAsync(insertOrMergeOperation);
        }

        public async Task<SrStoredSummarizedEpisode?> GetSummarizedEpisode(int episodeId)
        {
            await _episodeSummarizedTable.CreateIfNotExistsAsync();

            var retrieveOperation = TableOperation.Retrieve<SrStoredSummarizedEpisodeEntity>("SrStoredSummarizedEpisode", episodeId.ToString("D"));
            var result = await _episodeSummarizedTable.ExecuteAsync(retrieveOperation);
            var srStoredSummarizedEpisodeEntity = result.Result as SrStoredSummarizedEpisodeEntity;

            if (srStoredSummarizedEpisodeEntity == null)
            {
                return null;
            }

            return new SrStoredSummarizedEpisode
            {
                EpisodeId = srStoredSummarizedEpisodeEntity.EpisodeId,

                OriginalAudioUrl = srStoredSummarizedEpisodeEntity.OriginalAudioUrl,

                AudioUrl = srStoredSummarizedEpisodeEntity.AudioUrl,
                AudioLocale = srStoredSummarizedEpisodeEntity.AudioLocale,

                Title = srStoredSummarizedEpisodeEntity.Title,
                Description = srStoredSummarizedEpisodeEntity.Description,
                Url = srStoredSummarizedEpisodeEntity.Url,
                PublishDateUtc = srStoredSummarizedEpisodeEntity.PublishDateUtc,
                ImageUrl = srStoredSummarizedEpisodeEntity.ImageUrl,
                ProgramId = srStoredSummarizedEpisodeEntity.ProgramId,
                ProgramName = srStoredSummarizedEpisodeEntity.ProgramName,

                Transcription = srStoredSummarizedEpisodeEntity.Transcription,

                Title_Original = srStoredSummarizedEpisodeEntity.Title_Original,
                Description_Original = srStoredSummarizedEpisodeEntity.Description_Original,
                Transcription_Original = srStoredSummarizedEpisodeEntity.Transcription_Original,

                Title_EN = srStoredSummarizedEpisodeEntity.Title_EN,
                Description_EN = srStoredSummarizedEpisodeEntity.Description_EN,
                Transcription_EN = srStoredSummarizedEpisodeEntity.Transcription_EN,
                SpeechUrl_EN = srStoredSummarizedEpisodeEntity.SpeechUrl_EN,

                Title_SV = srStoredSummarizedEpisodeEntity.Title_SV,
                Description_SV = srStoredSummarizedEpisodeEntity.Description_SV,
                Transcription_SV = srStoredSummarizedEpisodeEntity.Transcription_SV,
                SpeechUrl_SV = srStoredSummarizedEpisodeEntity.SpeechUrl_SV
            };
        }

        public async Task StoreSummarizedEpisode(int episodeId, SrStoredSummarizedEpisode episode)
        {
            await _episodeSummarizedTable.CreateIfNotExistsAsync();

            var entity = new SrStoredSummarizedEpisodeEntity(episodeId, episode);
            var insertOrMergeOperation = TableOperation.InsertOrMerge(entity);
            await _episodeSummarizedTable.ExecuteAsync(insertOrMergeOperation);
        }

        public async Task<SrStoredEpisodeSpeech?> GetEpisodeSpeech(int episodeId)
        {
            await _episodeSpeechTable.CreateIfNotExistsAsync();

            var retrieveOperation = TableOperation.Retrieve<SrStoredEpisodeSpeechEntity>("SrStoredEpisodeSpeech", episodeId.ToString("D"));
            var result = await _episodeSpeechTable.ExecuteAsync(retrieveOperation);
            var srStoredEpisodeSpeechEntity = result.Result as SrStoredEpisodeSpeechEntity;

            if (srStoredEpisodeSpeechEntity == null)
            {
                return null;
            }

            return new SrStoredEpisodeSpeech
            {
                EpisodeId = srStoredEpisodeSpeechEntity.EpisodeId,

                SpeechBlobIdenitifier_SV = srStoredEpisodeSpeechEntity.SpeechBlobIdenitifier_SV,
                SpeechUrl_SV = srStoredEpisodeSpeechEntity.SpeechUrl_SV,

                SpeechBlobIdenitifier_EN = srStoredEpisodeSpeechEntity.SpeechBlobIdenitifier_EN,
                SpeechUrl_EN = srStoredEpisodeSpeechEntity.SpeechUrl_EN
            };
        }

        public async Task StoreEpisodeSpeech(int episodeId, SrStoredEpisodeSpeech episode)
        {
            await _episodeSpeechTable.CreateIfNotExistsAsync();

            var entity = new SrStoredEpisodeSpeechEntity(episodeId, episode);
            var insertOrMergeOperation = TableOperation.InsertOrMerge(entity);
            await _episodeSpeechTable.ExecuteAsync(insertOrMergeOperation);
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

        public class SrStoredEnrichedEpisodeEntity : TableEntity
        {
            public SrStoredEnrichedEpisodeEntity()
            {
            }

            public SrStoredEnrichedEpisodeEntity(int episodeId, SrStoredEnrichedEpisode enrichedEpisode)
            {
                PartitionKey = "SrStoredEnrichedEpisode";
                RowKey = episodeId.ToString("D");

                EpisodeId = episodeId;

                OriginalLocale = enrichedEpisode.OriginalLocale;

                Title_Original = enrichedEpisode.Title_Original;
                Description_Original = enrichedEpisode.Description_Original;
                Transcription_Original = enrichedEpisode.Transcription_Original;

                Title_EN = enrichedEpisode.Title_EN;
                Description_EN = enrichedEpisode.Description_EN;
                Transcription_EN = enrichedEpisode.Transcription_EN;

                Title_SV = enrichedEpisode.Title_SV;
                Description_SV = enrichedEpisode.Description_SV;
                Transcription_SV = enrichedEpisode.Transcription_SV;
            }

            public int EpisodeId { get; set; }

            public string OriginalLocale { get; set; } = "";

            [IgnoreProperty]
            public EnrichedText? Title_Original
            {
                get => JsonSerializer.Deserialize<EnrichedText>(Title_Original_Json);
                set => Title_Original_Json = JsonSerializer.Serialize(value);
            }
            public string Title_Original_Json { get; set; } = string.Empty;

            [IgnoreProperty]
            public EnrichedText? Description_Original
            {
                get => JsonSerializer.Deserialize<EnrichedText>(Description_Original_Json);
                set => Description_Original_Json = JsonSerializer.Serialize(value);
            }
            public string Description_Original_Json { get; set; } = string.Empty;

            [IgnoreProperty]
            public EnrichedText? Transcription_Original
            {
                get => JsonSerializer.Deserialize<EnrichedText>(Transcription_Original_Json);
                set => Transcription_Original_Json = JsonSerializer.Serialize(value);
            }
            public string Transcription_Original_Json { get; set; } = string.Empty;

            [IgnoreProperty]
            public EnrichedText? Title_EN
            {
                get => JsonSerializer.Deserialize<EnrichedText>(Title_EN_Json);
                set => Title_EN_Json = JsonSerializer.Serialize(value);
            }
            public string Title_EN_Json { get; set; } = string.Empty;

            [IgnoreProperty]
            public EnrichedText? Description_EN
            {
                get => JsonSerializer.Deserialize<EnrichedText>(Description_EN_Json);
                set => Description_EN_Json = JsonSerializer.Serialize(value);
            }
            public string Description_EN_Json { get; set; } = string.Empty;

            [IgnoreProperty]
            public EnrichedText? Transcription_EN
            {
                get => JsonSerializer.Deserialize<EnrichedText>(Transcription_EN_Json);
                set => Transcription_EN_Json = JsonSerializer.Serialize(value);
            }
            public string Transcription_EN_Json { get; set; } = string.Empty;


            [IgnoreProperty]
            public EnrichedText? Title_SV
            {
                get => JsonSerializer.Deserialize<EnrichedText>(Title_SV_Json);
                set => Title_SV_Json = JsonSerializer.Serialize(value);
            }
            public string Title_SV_Json { get; set; } = string.Empty;

            [IgnoreProperty]
            public EnrichedText? Description_SV
            {
                get => JsonSerializer.Deserialize<EnrichedText>(Description_SV_Json);
                set => Description_SV_Json = JsonSerializer.Serialize(value);
            }
            public string Description_SV_Json { get; set; } = string.Empty;

            [IgnoreProperty]
            public EnrichedText? Transcription_SV
            {
                get => JsonSerializer.Deserialize<EnrichedText>(Transcription_SV_Json);
                set => Transcription_SV_Json = JsonSerializer.Serialize(value);
            }
            public string Transcription_SV_Json { get; set; } = string.Empty;
        }

        public class SrStoredSummarizedEpisodeEntity : TableEntity
        {
            public SrStoredSummarizedEpisodeEntity()
            {
            }

            public SrStoredSummarizedEpisodeEntity(int episodeId, SrStoredSummarizedEpisode episode)
            {
                PartitionKey = "SrStoredSummarizedEpisode";
                RowKey = episodeId.ToString("D");

                EpisodeId = episodeId;

                OriginalAudioUrl = episode.OriginalAudioUrl;
                AudioUrl = episode.AudioUrl;
                AudioLocale = episode.AudioLocale;

                Title = episode.Title;
                Description = episode.Description;
                Url = episode.Url;
                PublishDateUtc = episode.PublishDateUtc;
                ImageUrl = episode.ImageUrl;
                ProgramId = episode.ProgramId;
                ProgramName = episode.ProgramName;
                Transcription = episode.Transcription;

                Title_Original = episode.Title_Original;
                Description_Original = episode.Description_Original;
                Transcription_Original = episode.Transcription_Original;

                Title_EN = episode.Title_EN;
                Description_EN = episode.Description_EN;
                Transcription_EN = episode.Transcription_EN;
                SpeechUrl_EN = episode.SpeechUrl_EN;

                Title_SV = episode.Title_SV;
                Description_SV = episode.Description_SV;
                Transcription_SV = episode.Transcription_SV;
                SpeechUrl_SV = episode.SpeechUrl_SV;
            }

            public int EpisodeId { get; set; }

            public string OriginalAudioUrl { get; set; } = string.Empty;

            public string AudioUrl { get; set; } = string.Empty;
            public string AudioLocale { get; set; } = string.Empty;

            public string Title { get; set; } = string.Empty;
            public string Description { get; set; } = string.Empty;
            public string Url { get; set; } = string.Empty;
            public DateTime PublishDateUtc { get; set; }

            public string ImageUrl { get; set; } = string.Empty;

            public int ProgramId { get; set; }
            public string ProgramName { get; set; } = string.Empty;

            public string Transcription { get; set; } = string.Empty;

            [IgnoreProperty]
            public EnrichedText? Title_Original
            {
                get => JsonSerializer.Deserialize<EnrichedText>(Title_Original_Json);
                set => Title_Original_Json = JsonSerializer.Serialize(value);
            }
            public string Title_Original_Json { get; set; } = string.Empty;

            [IgnoreProperty]
            public EnrichedText? Description_Original
            {
                get => JsonSerializer.Deserialize<EnrichedText>(Description_Original_Json);
                set => Description_Original_Json = JsonSerializer.Serialize(value);
            }
            public string Description_Original_Json { get; set; } = string.Empty;

            [IgnoreProperty]
            public EnrichedText? Transcription_Original
            {
                get => JsonSerializer.Deserialize<EnrichedText>(Transcription_Original_Json);
                set => Transcription_Original_Json = JsonSerializer.Serialize(value);
            }
            public string Transcription_Original_Json { get; set; } = string.Empty;

            [IgnoreProperty]
            public EnrichedText? Title_EN
            {
                get => JsonSerializer.Deserialize<EnrichedText>(Title_EN_Json);
                set => Title_EN_Json = JsonSerializer.Serialize(value);
            }
            public string Title_EN_Json { get; set; } = string.Empty;

            [IgnoreProperty]
            public EnrichedText? Description_EN
            {
                get => JsonSerializer.Deserialize<EnrichedText>(Description_EN_Json);
                set => Description_EN_Json = JsonSerializer.Serialize(value);
            }
            public string Description_EN_Json { get; set; } = string.Empty;

            [IgnoreProperty]
            public EnrichedText? Transcription_EN
            {
                get => JsonSerializer.Deserialize<EnrichedText>(Transcription_EN_Json);
                set => Transcription_EN_Json = JsonSerializer.Serialize(value);
            }
            public string Transcription_EN_Json { get; set; } = string.Empty;

            public string SpeechUrl_EN { get; set; } = string.Empty;


            [IgnoreProperty]
            public EnrichedText? Title_SV
            {
                get => JsonSerializer.Deserialize<EnrichedText>(Title_SV_Json);
                set => Title_SV_Json = JsonSerializer.Serialize(value);
            }
            public string Title_SV_Json { get; set; } = string.Empty;

            [IgnoreProperty]
            public EnrichedText? Description_SV
            {
                get => JsonSerializer.Deserialize<EnrichedText>(Description_SV_Json);
                set => Description_SV_Json = JsonSerializer.Serialize(value);
            }
            public string Description_SV_Json { get; set; } = string.Empty;

            [IgnoreProperty]
            public EnrichedText? Transcription_SV
            {
                get => JsonSerializer.Deserialize<EnrichedText>(Transcription_SV_Json);
                set => Transcription_SV_Json = JsonSerializer.Serialize(value);
            }
            public string Transcription_SV_Json { get; set; } = string.Empty;

            public string SpeechUrl_SV { get; set; } = string.Empty;
        }

        public class SrStoredEpisodeSpeechEntity : TableEntity
        {
            public SrStoredEpisodeSpeechEntity()
            {
            }

            public SrStoredEpisodeSpeechEntity(int episodeId, SrStoredEpisodeSpeech episode)
            {
                PartitionKey = "SrStoredEpisodeSpeech";
                RowKey = episodeId.ToString("D");

                EpisodeId = episodeId;

                SpeechBlobIdenitifier_SV = episode.SpeechBlobIdenitifier_SV;
                SpeechUrl_SV = episode.SpeechUrl_SV;

                SpeechBlobIdenitifier_EN = episode.SpeechBlobIdenitifier_EN;
                SpeechUrl_EN = episode.SpeechUrl_EN;
            }

            public int EpisodeId { get; set; }

            public string SpeechBlobIdenitifier_SV { get; set; } = string.Empty;
            public string SpeechUrl_SV { get; set; } = string.Empty;

            public string SpeechBlobIdenitifier_EN { get; set; } = string.Empty;
            public string SpeechUrl_EN { get; set; } = string.Empty;
        }
    }
}

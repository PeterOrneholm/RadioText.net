using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.Azure.Cosmos.Table;

namespace Orneholm.RadioText.Core.Storage
{
    public class SummaryAzureTableStorage : ISummaryStorage
    {
        private readonly CloudTable _episodeSummarizedTable;

        public SummaryAzureTableStorage(CloudTableClient cloudTableClient, string episodeSummarizedTableName)
        {
            _episodeSummarizedTable = cloudTableClient.GetTableReference(episodeSummarizedTableName);
        }

        public async Task<List<SrStoredSummarizedEpisode>> ListSummarizedEpisode(int count = 100)
        {
            await _episodeSummarizedTable.CreateIfNotExistsAsync();

            var query = new TableQuery<SrStoredSummarizedEpisodeEntity> {TakeCount = count}
                .OrderByDesc("RowKey");

            TableContinuationToken? token = null;
            var items = new List<SrStoredSummarizedEpisode>();
            var finished = false;

            while (!finished)
            {
                var result = await _episodeSummarizedTable.ExecuteQuerySegmentedAsync(query, token);

                items.AddRange(result.Results.Select(Map));

                token = result.ContinuationToken;
                if (token == null)
                {
                    finished = true;
                }
            }

            return items.OrderByDescending(x => x.PublishDateUtc).Take(count).ToList();
        }

        public async Task<List<SrStoredMiniSummarizedEpisode>> ListMiniSummarizedEpisode(int count = 100)
        {
            await _episodeSummarizedTable.CreateIfNotExistsAsync();

            var query = new TableQuery<SrStoredSummarizedEpisodeEntity>
            {
                TakeCount = count,
                SelectColumns = new List<string>
                {
                    nameof(SrStoredSummarizedEpisodeEntity.PartitionKey),
                    nameof(SrStoredSummarizedEpisodeEntity.RowKey),
                    nameof(SrStoredSummarizedEpisodeEntity.EpisodeId),
                    nameof(SrStoredSummarizedEpisodeEntity.OriginalAudioUrl),
                    nameof(SrStoredSummarizedEpisodeEntity.Title),
                    nameof(SrStoredSummarizedEpisodeEntity.Title_EN_Json),
                    nameof(SrStoredSummarizedEpisodeEntity.Description),
                    nameof(SrStoredSummarizedEpisodeEntity.Url),
                    nameof(SrStoredSummarizedEpisodeEntity.PublishDateUtc),
                    nameof(SrStoredSummarizedEpisodeEntity.ImageUrl),
                    nameof(SrStoredSummarizedEpisodeEntity.ProgramId),
                    nameof(SrStoredSummarizedEpisodeEntity.ProgramName),
                    nameof(SrStoredSummarizedEpisodeEntity.Transcription_Original_Json),
                    nameof(SrStoredSummarizedEpisodeEntity.Transcription_EN_Json),
                    nameof(SrStoredSummarizedEpisodeEntity.SpeechUrl_EN),
                }
            }.OrderByDesc("RowKey");

            var requestOptions = new TableRequestOptions {TableQueryMaxItemCount = count};

            TableContinuationToken? token = null;
            var items = new List<SrStoredMiniSummarizedEpisode>();
            var finished = false;

            while (!finished)
            {
                var result =
                    await _episodeSummarizedTable.ExecuteQuerySegmentedAsync(query, token, requestOptions, null);

                items.AddRange(result.Results.Select(MapMini));

                token = result.ContinuationToken;
                if (token == null || items.Count >= count)
                {
                    finished = true;
                }
            }

            return items.OrderByDescending(x => x.PublishDateUtc).Take(count).ToList();
        }

        public async Task<SrStoredSummarizedEpisode?> GetSummarizedEpisode(int episodeId)
        {
            await _episodeSummarizedTable.CreateIfNotExistsAsync();

            var retrieveOperation =
                TableOperation.Retrieve<SrStoredSummarizedEpisodeEntity>("SrStoredSummarizedEpisode",
                    episodeId.ToString("D"));
            var result = await _episodeSummarizedTable.ExecuteAsync(retrieveOperation);
            var srStoredSummarizedEpisodeEntity = result.Result as SrStoredSummarizedEpisodeEntity;

            if (srStoredSummarizedEpisodeEntity == null)
            {
                return null;
            }

            return Map(srStoredSummarizedEpisodeEntity);
        }

        private static SrStoredSummarizedEpisode Map(SrStoredSummarizedEpisodeEntity srStoredSummarizedEpisodeEntity)
        {
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

        private static SrStoredMiniSummarizedEpisode MapMini(
            SrStoredSummarizedEpisodeEntity srStoredSummarizedEpisodeEntity)
        {
            return new SrStoredMiniSummarizedEpisode
            {
                EpisodeId = srStoredSummarizedEpisodeEntity.EpisodeId,
                OriginalAudioUrl = srStoredSummarizedEpisodeEntity.OriginalAudioUrl,
                Title = srStoredSummarizedEpisodeEntity.Title,
                Title_EN = srStoredSummarizedEpisodeEntity.Title_EN,
                Description = srStoredSummarizedEpisodeEntity.Description,
                Url = srStoredSummarizedEpisodeEntity.Url,
                PublishDateUtc = srStoredSummarizedEpisodeEntity.PublishDateUtc,
                ImageUrl = srStoredSummarizedEpisodeEntity.ImageUrl,
                ProgramId = srStoredSummarizedEpisodeEntity.ProgramId,
                ProgramName = srStoredSummarizedEpisodeEntity.ProgramName,
                Transcription_Original = srStoredSummarizedEpisodeEntity.Transcription_Original,
                Transcription_English = srStoredSummarizedEpisodeEntity.Transcription_EN,
                SpeechUrl_EN = srStoredSummarizedEpisodeEntity.SpeechUrl_EN
            };
        }

        public async Task StoreSummarizedEpisode(int episodeId, SrStoredSummarizedEpisode episode)
        {
            await _episodeSummarizedTable.CreateIfNotExistsAsync();

            var entity = new SrStoredSummarizedEpisodeEntity(episodeId, episode);
            var insertOrMergeOperation = TableOperation.InsertOrMerge(entity);
            await _episodeSummarizedTable.ExecuteAsync(insertOrMergeOperation);
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
    }
}

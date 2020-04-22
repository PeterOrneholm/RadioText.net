using System;
using System.Collections.Generic;

namespace Orneholm.RadioText.Core.Storage
{
    public class SrStoredSummarizedEpisode
    {
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


        public EnrichedText? Title_Original { get; set; }
        public EnrichedText? Description_Original { get; set; }
        public EnrichedText? Transcription_Original { get; set; }

        public EnrichedText? Title_EN { get; set; }
        public EnrichedText? Description_EN { get; set; }
        public EnrichedText? Transcription_EN { get; set; }
        public string SpeechUrl_EN { get; set; } = string.Empty;

        public EnrichedText? Title_SV { get; set; }
        public EnrichedText? Description_SV { get; set; }
        public EnrichedText? Transcription_SV { get; set; }
        public string SpeechUrl_SV { get; set; } = string.Empty;
    }
}

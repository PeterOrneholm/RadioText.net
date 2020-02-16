namespace Orneholm.RadioText.Core.Storage
{
    public class SrStoredEnrichedEpisode
    {
        public int EpisodeId { get; set; }

        public string OriginalLocale { get; set; } = "";

        public EnrichedText? Title_Original { get; set; }
        public EnrichedText? Description_Original { get; set; }
        public EnrichedText? Transcription_Original { get; set; }

        public EnrichedText? Title_EN { get; set; }
        public EnrichedText? Description_EN { get; set; }
        public EnrichedText? Transcription_EN { get; set; }

        public EnrichedText? Title_SV { get; set; }
        public EnrichedText? Description_SV { get; set; }
        public EnrichedText? Transcription_SV { get; set; }
    }
}

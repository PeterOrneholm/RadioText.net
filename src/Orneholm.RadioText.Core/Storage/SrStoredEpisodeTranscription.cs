namespace Orneholm.RadioText.Core.Storage
{
    public class SrStoredEpisodeTranscription
    {
        public int EpisodeId { get; set; }

        public string Status { get; set; } = "Unknown";

        public string TranscriptionResultChannel0BlobIdentifier { get; set; } = string.Empty;
        public string TranscriptionResultChannel0Url { get; set; } = string.Empty;

        public string TranscriptionResultChannel1BlobIdentifier { get; set; } = string.Empty;
        public string TranscriptionResultChannel1Url { get; set; } = string.Empty;

        public string CombinedDisplayResult { get; set; } = string.Empty;
    }
}

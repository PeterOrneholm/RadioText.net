namespace Orneholm.RadioText.Azure.SpeechBatchClient
{
    public class AudioFileResult
    {
        public string AudioFileName { get; set; } = string.Empty;
        public string AudioFileUrl { get; set; } = string.Empty;
        public float AudioLengthInSeconds { get; set; }
        public CombinedResult[] CombinedResults { get; set; } = { };
        public SegmentResult[] SegmentResults { get; set; } = { };
    }
}

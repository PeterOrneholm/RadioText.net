using System.Collections.Generic;

namespace Orneholm.RadioText.Azure.SpeechBatchClient
{
    public class AudioFileResult
    {
        public string AudioFileName { get; set; } = string.Empty;
        public List<SegmentResult> SegmentResults { get; set; } = new List<SegmentResult>();
    }
}

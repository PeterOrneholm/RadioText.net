using System.Collections.Generic;

namespace Orneholm.RadioText.Azure.SpeechBatchClient
{
    public class SegmentResult
    {
        public string RecognitionStatus { get; set; } = string.Empty;
        public string Offset { get; set; } = string.Empty;
        public string Duration { get; set; } = string.Empty;
        public List<NBest> NBest { get; set; } = new List<NBest>();
    }
}

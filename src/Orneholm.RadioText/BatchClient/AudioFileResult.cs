using System.Collections.Generic;

namespace Orneholm.RadioText.BatchClient
{
    public class AudioFileResult
    {
        public string AudioFileName { get; set; }
        public List<SegmentResult> SegmentResults { get; set; }
    }
}
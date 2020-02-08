using System.Collections.Generic;

namespace Orneholm.RadioText.Azure.SpeechBatchClient
{
    public class RootObject
    {
        public List<AudioFileResult> AudioFileResults { get; set; } = new List<AudioFileResult>();
    }
}

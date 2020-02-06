using System.Collections.Generic;

namespace Orneholm.RadioText
{
    public class TransferBlob
    {
        public string TargetBlobIdentifier { get; set; }
        public Dictionary<string, string> TargetBlobMetadata { get; set; } = null;
        public string SourceUrl { get; set; } = null;
    }
}
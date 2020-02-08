using System.Collections.Generic;

namespace Orneholm.RadioText.Core.Storage
{
    public class TransferBlob
    {
        public string TargetBlobIdentifier { get; set; } = string.Empty;
        public Dictionary<string, string> TargetBlobMetadata { get; set; } = new Dictionary<string, string>();
        public string SourceUrl { get; set; } = string.Empty;
    }
}

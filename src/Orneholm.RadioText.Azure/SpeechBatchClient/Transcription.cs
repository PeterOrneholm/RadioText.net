using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace Orneholm.RadioText.Azure.SpeechBatchClient
{
    public sealed class Transcription
    {
        public string Name { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string Locale { get; set; } = string.Empty;
        public Uri? RecordingsUrl { get; set; }
        public IReadOnlyDictionary<string, string> ResultsUrls { get; set; } = new ReadOnlyDictionary<string, string>(new Dictionary<string, string>());
        public Guid Id { get; set; }
        public DateTime CreatedDateTime { get; set; }
        public DateTime LastActionDateTime { get; set; }
        public string Status { get; set; } = string.Empty;
        public string StatusMessage { get; set; } = string.Empty;
    }
}

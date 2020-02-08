using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Orneholm.RadioText.Azure.SpeechBatchClient
{
    public interface ISpeechBatchClient
    {
        Task<IEnumerable<Transcription>> GetTranscriptionsAsync();
        Task<Transcription> GetTranscriptionAsync(Guid id);
        Task<Uri> PostTranscriptionAsync(string name, string description, string locale, Uri recordingsUrl);
        Task<Uri> PostTranscriptionAsync(string name, string description, string locale, Uri recordingsUrl, IEnumerable<Guid> modelIds);
        Task<Transcription> GetTranscriptionAsync(Uri location);
        Task DeleteTranscriptionAsync(Guid id);
    }
}

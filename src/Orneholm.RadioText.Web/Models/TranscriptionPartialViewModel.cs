using Orneholm.RadioText.Core.Storage;

namespace Orneholm.RadioText.Web.Models
{
    public class TranscriptionPartialViewModel
    {
        public string Title { get; set; }
        public EnrichedText EnrichedText { get; set; }
        public string AudioUrl { get; set; }
    }
}

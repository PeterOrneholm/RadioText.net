using System.Collections.Generic;
using Microsoft.Azure.CognitiveServices.Language.TextAnalytics.Models;

namespace Orneholm.RadioText.Core.Storage
{
    public class EnrichedText
    {
        public string Text { get; set; } = string.Empty;

        public List<string> KeyPhrases { get; set; } = new List<string>();
        public List<EntityRecord> Entities { get; set; } = new List<EntityRecord>();

        public double? Sentiment { get; set; }
    }
}

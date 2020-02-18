using System.Collections.Generic;
using System.Linq;
using Microsoft.Azure.CognitiveServices.Language.TextAnalytics.Models;

namespace Orneholm.RadioText.Core.Storage
{
    public class EnrichedText
    {
        public string Text { get; set; } = string.Empty;

        public List<string> KeyPhrases { get; set; } = new List<string>();
        public List<EntityRecord> Entities { get; set; } = new List<EntityRecord>();

        public double? Sentiment { get; set; }

        public Dictionary<string, List<EntityRecord>> GetEntitiesByCategory()
        {
            return Entities.GroupBy(x => $"{x.Type}" + (!string.IsNullOrWhiteSpace(x.SubType) ? $" ({x.SubType})" : ""))
                .OrderBy(x => x.Key)
                .ToDictionary(x => x.Key, x => x.ToList());
        }
    }
}

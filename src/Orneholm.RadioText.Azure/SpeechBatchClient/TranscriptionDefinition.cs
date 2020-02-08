using System;
using System.Collections.Generic;

namespace Orneholm.RadioText.Azure.SpeechBatchClient
{
    public sealed class TranscriptionDefinition
    {
        private TranscriptionDefinition(string name, string description, string locale, Uri recordingsUrl, IEnumerable<ModelIdentity> models)
        {
            Name = name;
            Description = description;
            RecordingsUrl = recordingsUrl;
            Locale = locale;
            Models = models;
            Properties = new Dictionary<string, string>
            {
                {"PunctuationMode", "DictatedAndAutomatic"},
                {"ProfanityFilterMode", "None"},
                {"AddWordLevelTimestamps", "True"},
                {"AddSentiment", "False"}
            };
        }

        public string Name { get; set; }
        public string Description { get; set; }
        public Uri RecordingsUrl { get; set; }
        public string Locale { get; set; }
        public IEnumerable<ModelIdentity> Models { get; set; }
        public IDictionary<string, string> Properties { get; set; }

        public static TranscriptionDefinition Create(
            string name,
            string description,
            string locale,
            Uri recordingsUrl)
        {
            return TranscriptionDefinition.Create(name, description, locale, recordingsUrl, new ModelIdentity[0]);
        }

        public static TranscriptionDefinition Create(
            string name,
            string description,
            string locale,
            Uri recordingsUrl,
            IEnumerable<ModelIdentity> models)
        {
            return new TranscriptionDefinition(name, description, locale, recordingsUrl, models);
        }
    }
}

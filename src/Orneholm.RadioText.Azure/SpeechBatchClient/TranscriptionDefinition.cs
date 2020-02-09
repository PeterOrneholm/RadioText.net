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
        }

        public string Name { get; set; }
        public string Description { get; set; }
        public Uri RecordingsUrl { get; set; }
        public string Locale { get; set; }
        public IEnumerable<ModelIdentity> Models { get; set; }

        public PunctuationMode PunctuationMode { get; set; } = PunctuationMode.DictatedAndAutomatic;
        public ProfanityFilterMode ProfanityFilterMode { get; set; } = ProfanityFilterMode.None;
        public bool AddWordLevelTimestamps { get; set; } = true;
        public bool AddSentiment { get; set; } = false;
        public bool AddDiarization { get; set; } = false;
        public string TranscriptionResultsContainerUrl { get; set; } = string.Empty;

        public IDictionary<string, string> Properties
        {
            get
            {
                var properties = new Dictionary<string, string>
                {
                    { "PunctuationMode", PunctuationMode.ToString() },
                    { "ProfanityFilterMode", ProfanityFilterMode.ToString() },
                    { "AddWordLevelTimestamps", AddWordLevelTimestamps.ToString() },
                    { "AddSentiment", AddSentiment.ToString() },
                    { "AddDiarization", AddDiarization.ToString() }
                };

                if (!string.IsNullOrWhiteSpace(TranscriptionResultsContainerUrl))
                {
                    properties["TranscriptionResultsContainerUrl"] = TranscriptionResultsContainerUrl;
                }

                return properties;
            }
        }

        public static TranscriptionDefinition Create(
            string name,
            string description,
            string locale,
            Uri recordingsUrl)
        {
            return Create(name, description, locale, recordingsUrl, new ModelIdentity[0]);
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

    public enum PunctuationMode
    {
        None,
        Dictated,
        Automatic,
        DictatedAndAutomatic
    }

    public enum ProfanityFilterMode
    {
        None,
        Removed,
        Tags,
        Masked
    }
}

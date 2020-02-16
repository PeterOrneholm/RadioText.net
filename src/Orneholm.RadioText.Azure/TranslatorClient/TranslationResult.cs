using System.Collections.Generic;

namespace Orneholm.RadioText.Azure.TranslatorClient
{
    public class TranslationResult
    {
        public DetectedLanguage DetectedLanguage { get; set; } = new DetectedLanguage();
        public TextResult SourceText { get; set; } = new TextResult();
        public List<Translation> Translations { get; set; } = new List<Translation>();
    }
}
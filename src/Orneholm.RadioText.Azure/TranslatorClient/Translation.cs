namespace Orneholm.RadioText.Azure.TranslatorClient
{
    public class Translation
    {
        public string Text { get; set; } = string.Empty;
        public TextResult Transliteration { get; set; } = new TextResult();
        public string To { get; set; } = string.Empty;
        public Alignment Alignment { get; set; } = new Alignment();
        public SentenceLength SentLen { get; set; } = new SentenceLength();
    }
}
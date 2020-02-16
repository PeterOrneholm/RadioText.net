namespace Orneholm.RadioText.Azure.TranslatorClient
{
    public class DetectedLanguage
    {
        public string Language { get; set; } = string.Empty;
        public float Score { get; set; }
    }
}
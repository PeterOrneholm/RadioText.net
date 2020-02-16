namespace Orneholm.RadioText.Azure.TranslatorClient
{
    public class TranslationRequest
    {
        public TranslationRequest(string text)
        {
            Text = text;
        }

        public string Text { get; set; }
    }
}

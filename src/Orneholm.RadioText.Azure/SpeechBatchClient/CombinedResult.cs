namespace Orneholm.RadioText.Azure.SpeechBatchClient
{
    public class CombinedResult
    {
        public string ChannelNumber { get; set; } = string.Empty;
        public string Lexical { get; set; } = string.Empty;
        public string Itn { get; set; } = string.Empty;
        public string MaskedItn { get; set; } = string.Empty;
        public string Display { get; set; } = string.Empty;
    }
}

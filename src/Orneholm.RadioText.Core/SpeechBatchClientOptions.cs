namespace Orneholm.RadioText.Core
{
    public class SpeechBatchClientOptions
    {
        public string Key { get; set; } = string.Empty;
        public string Hostname { get; set; } = string.Empty;
        public string Region { get; set; } = string.Empty;
        public int Port { get; set; } = 443;
    }
}
namespace Orneholm.RadioText.Azure.SpeechBatchClient
{
    public class NBest
    {
        public double Confidence { get; set; } = 0.0;
        public string Lexical { get; set; } = string.Empty;
        public string ITN { get; set; } = string.Empty;
        public string MaskedITN { get; set; } = string.Empty;
        public string Display { get; set; } = string.Empty;
    }
}

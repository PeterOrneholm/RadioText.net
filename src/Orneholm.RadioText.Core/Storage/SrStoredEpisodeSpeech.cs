namespace Orneholm.RadioText.Core.Storage
{
    public class SrStoredEpisodeSpeech
    {
        public int EpisodeId { get; set; }

        public string SpeechBlobIdenitifier_SV { get; set; } = string.Empty;
        public string SpeechUrl_SV { get; set; } = string.Empty;

        public string SpeechBlobIdenitifier_EN { get; set; } = string.Empty;
        public string SpeechUrl_EN { get; set; } = string.Empty;
    }
}
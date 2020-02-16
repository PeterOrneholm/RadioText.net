using Orneholm.SverigesRadio.Api.Models.Response.Episodes;

namespace Orneholm.RadioText.Core.Storage
{
    public class SrStoredEpisode
    {
        public Episode Episode { get; set; } = new Episode();
        public string OriginalAudioUrl { get; set; } = string.Empty;
        public string ImageBlobIdentifier { get; set; } = string.Empty;
        public string AudioBlobIdentifier { get; set; } = string.Empty;
        public string AudioUrl { get; set; } = string.Empty;
        public string AudioExtension { get; set; } = string.Empty;
        public string AudioLocale { get; set; } = string.Empty;
    }

    public class SrStoredEpisodeSpeech
    {
        public int EpisodeId { get; set; }

        public string SpeechBlobIdenitifier_SV { get; set; } = string.Empty;
        public string SpeechUrl_SV { get; set; } = string.Empty;

        public string SpeechBlobIdenitifier_EN { get; set; } = string.Empty;
        public string SpeechUrl_EN { get; set; } = string.Empty;
    }
}

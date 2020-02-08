using Orneholm.SverigesRadio.Api.Models.Response.Episodes;

namespace Orneholm.RadioText.Core.Storage
{
    public class SrStoredEpisode
    {
        public Episode Episode { get; set; } = new Episode();
        public string OriginalAudioUrl { get; set; } = string.Empty;
        public string AudioBlobIdentifier { get; set; } = string.Empty;
        public string AudioUrl { get; set; } = string.Empty;
        public string AudioExtension { get; set; } = string.Empty;
        public string AudioLocale { get; set; } = string.Empty;
    }
}
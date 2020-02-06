using System;
using Orneholm.SverigesRadio.Api.Models.Response.Episodes;

namespace Orneholm.RadioText
{
    public class TransferedEpisode
    {
        public Episode Episode { get; set; }
        public string EpisodeAudioLocale { get; set; }
        public string EpisodeBlobIdentifier { get; set; }
        public Uri OriginalAudioUri { get; set; }
        public string OriginalAudioExtension { get; set; }
        public Uri BlobAudioAuthenticatedUri { get; set; }
        public Uri BlobAudioUri { get; set; }
    }
}
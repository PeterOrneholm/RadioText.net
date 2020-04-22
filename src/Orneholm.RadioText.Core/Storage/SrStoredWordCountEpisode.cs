using System;
using System.Collections.Generic;

namespace Orneholm.RadioText.Core.Storage
{
    public class SrStoredWordCountEpisode
    {
        public int EpisodeId { get; set; }

        public string EpisodeAudioUrl { get; set; } = string.Empty;
        public string EpisodeAudioLocale { get; set; } = string.Empty;

        public string EpisodeTitle { get; set; } = string.Empty;
        public string EpisodeUrl { get; set; } = string.Empty;
        public DateTime EpisodePublishDateUtc { get; set; }

        public int ProgramId { get; set; }
        public string ProgramName { get; set; } = string.Empty;

        public string EpisodeAudioTranscription { get; set; } = string.Empty;

        public Dictionary<string, int> WordCount { get; set; } = new Dictionary<string, int>();
    }
}

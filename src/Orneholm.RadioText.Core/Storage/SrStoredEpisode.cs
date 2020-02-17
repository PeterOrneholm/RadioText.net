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

    public class SrStoredEpisodeStatus
    {
        public int EpisodeId { get; set; }
        public string Phase { get; set; } = SrStoredEpisodePhases.Unknown.ToString();
        public string State { get; set; } = SrStoredEpisodeStates.Unknown;
        public string Info { get; set; } = string.Empty;

        public static SrStoredEpisodeStatus Unknown(int episodeId)
        {
            return new SrStoredEpisodeStatus
            {
                EpisodeId = episodeId,
                Phase = SrStoredEpisodePhases.Unknown.ToString(),
                State = SrStoredEpisodeStates.Unknown,
                Info = string.Empty
            };
        }

        public static SrStoredEpisodeStatus Started(int episodeId, SrStoredEpisodePhases phase)
        {
            return new SrStoredEpisodeStatus
            {
                EpisodeId = episodeId,
                Phase = phase.ToString(),
                State = SrStoredEpisodeStates.Started,
                Info = string.Empty
            };
        }

        public static SrStoredEpisodeStatus Done(int episodeId, SrStoredEpisodePhases phase)
        {
            return new SrStoredEpisodeStatus
            {
                EpisodeId = episodeId,
                Phase = phase.ToString(),
                State = SrStoredEpisodeStates.Done,
                Info = string.Empty
            };
        }

        public static SrStoredEpisodeStatus Error(int episodeId, SrStoredEpisodePhases phase, string exception)
        {
            return new SrStoredEpisodeStatus
            {
                EpisodeId = episodeId,
                Phase = phase.ToString(),
                State = SrStoredEpisodeStates.Error,
                Info = exception
            };
        }
    }

    public enum SrStoredEpisodePhases
    {
        Unknown = 0,

        Collect = 1,
        Transcribe = 2,
        Enrich = 3,
        GenerateSpeech = 4,
        Summarize = 5
    }


    public static class SrStoredEpisodeStates
    {
        public const string Unknown = "Unknown";

        public const string Started = "Started";
        public const string Done = "Done";
        public const string Error = "Error";
    }
}

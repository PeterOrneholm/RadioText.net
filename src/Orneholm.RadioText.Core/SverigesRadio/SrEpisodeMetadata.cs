using System.Collections.Generic;
using System.Linq;
using Orneholm.SverigesRadio.Api;
using Orneholm.SverigesRadio.Api.Models.Response.Episodes;

namespace Orneholm.RadioText.Core.SverigesRadio
{
    public static class SrEpisodeMetadata
    {
        private static string ProgramLocaleDefault = "sv-SE";
        private static readonly Dictionary<int, string> ProgramLocaleMapping = new Dictionary<int, string>
        {
            { SverigesRadioApiIds.Programs.RadioSweden, "en-US" }
        };

        public static string GetAudioLocaleForEpisode(Episode episode)
        {
            var programId = episode.Program.Id;
            if (ProgramLocaleMapping.ContainsKey(programId))
            {
                return ProgramLocaleMapping[programId];
            }

            return ProgramLocaleDefault;
        }

        public static string? GetFileUrl(Episode episode)
        {
            var fileUrl = episode?.DownloadPodfile?.Url;
            fileUrl ??= episode?.Broadcast?.BroadcastFiles.FirstOrDefault()?.Url;

            return fileUrl;
        }
    }
}

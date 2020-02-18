using System.Collections.Generic;
using Orneholm.RadioText.Core.Storage;

namespace Orneholm.RadioText.Web.Models
{
    public class HomeIndexViewModel
    {
        public List<SrStoredMiniSummarizedEpisode> Episodes { get; set; }
        public string SearchQuery { get; set; }
    }
}

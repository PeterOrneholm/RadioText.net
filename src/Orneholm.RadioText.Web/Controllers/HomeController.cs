using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Orneholm.RadioText.Core.Storage;
using Orneholm.RadioText.Web.Models;

namespace Orneholm.RadioText.Web.Controllers
{
    public class HomeController : Controller
    {
        private readonly ISummaryStorage _summaryStorage;

        public HomeController(ISummaryStorage summaryStorage)
        {
            _summaryStorage = summaryStorage;
        }

        public async Task<IActionResult> Index(string entityName = null, string entityType = null, string keyphrase = null)
        {
            var episodes = await _summaryStorage.ListSummarizedEpisode(100);

            var filteredEpisodes = FilterEpisodes(entityName, entityType, keyphrase, episodes);
            var orderedEpisodes = OrderEpisodes(filteredEpisodes);

            return View(new HomeIndexViewModel()
            {
                Episodes = orderedEpisodes.ToList()
            });
        }

        private static IOrderedEnumerable<SrStoredSummarizedEpisode> OrderEpisodes(List<SrStoredSummarizedEpisode> filteredEpisodes)
        {
            return filteredEpisodes.OrderByDescending(x => x.PublishDateUtc);
        }

        private static List<SrStoredSummarizedEpisode> FilterEpisodes(string entityName, string entityType, string keyphrase, List<SrStoredSummarizedEpisode> episodes)
        {
            var filteredEpisodes = episodes;
            if (!string.IsNullOrWhiteSpace(entityName))
            {
                filteredEpisodes = filteredEpisodes
                    .Where(x => x.Transcription_Original.Entities.Any(y => y.Name == entityName && (string.IsNullOrWhiteSpace(entityType) || y.Type == entityType)))
                    .ToList();
            }

            if (!string.IsNullOrWhiteSpace(keyphrase))
            {
                filteredEpisodes = filteredEpisodes
                    .Where(x => x.Transcription_Original.KeyPhrases.Contains(keyphrase))
                    .ToList();
            }

            return filteredEpisodes;
        }
    }
}

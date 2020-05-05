using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Clients.ActiveDirectory;
using Orneholm.RadioText.Core.Storage;
using Orneholm.RadioText.Web.Models;

namespace Orneholm.RadioText.Web.Controllers
{
    public class HomeController : Controller
    {
        private const int EpisodesListCount = 40;

        private readonly ISummaryStorage _summaryStorage;
        private readonly IEpisodeLister _episodeLister;
        private readonly ImmersiveReaderOptions _immersiveReaderOptions;

        public HomeController(IOptions<ImmersiveReaderOptions> immersiveReaderOptions, IEpisodeLister episodeLister, ISummaryStorage summaryStorage)
        {
            _episodeLister = episodeLister;
            _summaryStorage = summaryStorage;
            _immersiveReaderOptions = immersiveReaderOptions.Value;
        }

        [Route("/")]
        [ResponseCache(Duration = 60, Location = ResponseCacheLocation.Any, NoStore = false)]
        public async Task<IActionResult> Index(string entityName = null, string entityType = null, string keyphrase = null, string query = null, int? programId = null)
        {
            var episodes = await _episodeLister.List(EpisodesListCount, entityName, entityType, keyphrase, query, programId);
            return View(new HomeIndexViewModel
            {
                SearchQuery = query,
                Episodes = episodes.ToList()
            });
        }

        [Route("/episode/{id}")]
        [ResponseCache(Duration = 60, Location = ResponseCacheLocation.Any, NoStore = false)]
        public async Task<IActionResult> Details(int id)
        {
            var episode = await _summaryStorage.GetSummarizedEpisode(id);

            return View(new HomeDetailsViewModel
            {
                Episode = episode
            });
        }

        [HttpGet("/api/immersivereader/token")]
        public async Task<JsonResult> GetTokenAndSubdomain()
        {
            var tokenResult = await GetTokenAsync();

            return new JsonResult(new
            {
                token = tokenResult,
                subdomain = _immersiveReaderOptions.Subdomain
            });
        }

        private async Task<string> GetTokenAsync()
        {
            var authority = $"https://login.windows.net/{_immersiveReaderOptions.TenantId}";
            const string resource = "https://cognitiveservices.azure.com/";

            var authContext = new AuthenticationContext(authority);
            var clientCredential = new ClientCredential(_immersiveReaderOptions.ClientId, _immersiveReaderOptions.ClientSecret);

            var authResult = await authContext.AcquireTokenAsync(resource, clientCredential);

            return authResult.AccessToken;
        }
    }
}

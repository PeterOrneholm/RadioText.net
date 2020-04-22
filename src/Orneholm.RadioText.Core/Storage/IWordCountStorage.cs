using System.Threading.Tasks;

namespace Orneholm.RadioText.Core.Storage
{
    public interface IWordCountStorage
    {
        Task StoreWordCounterEpisode(int episodeId, SrStoredWordCountEpisode episode);
    }
}

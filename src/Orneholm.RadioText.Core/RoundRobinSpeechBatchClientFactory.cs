using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Orneholm.RadioText.Azure.SpeechBatchClient;

namespace Orneholm.RadioText.Core
{
    public class RoundRobinSpeechBatchClientFactory : ISpeechBatchClientFactory
    {
        private readonly object _lock = new object();
        private readonly SpeechBatchClient[] _clients;
        private int _index;

        public RoundRobinSpeechBatchClientFactory(List<SpeechBatchClientOptions> options)
        {
            _clients = options.Select(x => SpeechBatchClient.CreateApiV2Client(x.Key, x.Hostname, x.Port)).ToArray();
        }

        public SpeechBatchClient Get()
        {
            lock (_lock)
            {
                var client = _clients[_index];

                _index += 1;
                _index = _index % _clients.Length;

                return client;
            }
        }

        public async Task CleanExistingTranscriptions()
        {
            foreach (var speechBatchClient in _clients)
            {
                var transcriptions = await speechBatchClient.GetTranscriptionsAsync();
                foreach (var transcription in transcriptions)
                {
                    await speechBatchClient.DeleteTranscriptionAsync(transcription.Id);
                }
            }
        }
    }
}

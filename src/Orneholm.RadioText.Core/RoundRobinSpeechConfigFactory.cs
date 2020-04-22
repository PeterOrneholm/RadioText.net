using System.Collections.Generic;
using System.Linq;
using Microsoft.CognitiveServices.Speech;

namespace Orneholm.RadioText.Core
{
    public class RoundRobinSpeechConfigFactory : ISpeechConfigFactory
    {
        private readonly object _lock = new object();
        private readonly SpeechConfig[] _clients;
        private int _index;

        public RoundRobinSpeechConfigFactory(List<SpeechBatchClientOptions> options)
        {
            _clients = options.Select(x => SpeechConfig.FromSubscription(x.Key, x.Region)).ToArray();
        }

        public SpeechConfig Get()
        {
            lock (_lock)
            {
                var config = _clients[_index];

                _index += 1;
                _index = _index % _clients.Length;

                return config;
            }
        }
    }
}
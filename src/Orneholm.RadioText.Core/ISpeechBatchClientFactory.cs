using Orneholm.RadioText.Azure.SpeechBatchClient;

namespace Orneholm.RadioText.Core
{
    public interface ISpeechBatchClientFactory
    {
        SpeechBatchClient Get();
    }
}

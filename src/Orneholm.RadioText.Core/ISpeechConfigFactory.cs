using Microsoft.CognitiveServices.Speech;

namespace Orneholm.RadioText.Core
{
    public interface ISpeechConfigFactory
    {
        SpeechConfig Get();
    }
}
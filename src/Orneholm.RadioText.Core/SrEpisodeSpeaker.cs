using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using Microsoft.Azure.Storage.Blob;
using Microsoft.Extensions.Logging;
using Orneholm.RadioText.Core.Storage;
using Microsoft.CognitiveServices.Speech;
using Microsoft.CognitiveServices.Speech.Audio;
using Orneholm.SverigesRadio.Api.Models.Response.Episodes;

namespace Orneholm.RadioText.Core
{
    public class SrEpisodeSpeaker
    {
        private const string EnUsVoice = "en-US-AriaNeural";
        private const string SvSeVoice = "sv-SE-HedvigRUS";

        private readonly CloudBlobContainer _speakerContainer;
        private readonly SpeechConfig _speechConfig;
        private readonly IStorage _storage;
        private readonly ILogger<SrEpisodeSpeaker> _logger;

        public SrEpisodeSpeaker(string speakerContainerName, SpeechConfig speechConfig, IStorage storage, ILogger<SrEpisodeSpeaker> logger, CloudBlobClient cloudBlobClient)
        {
            _speakerContainer = cloudBlobClient.GetContainerReference(speakerContainerName);
            _speechConfig = speechConfig;
            _storage = storage;
            _logger = logger;
        }

        public async Task GenerateSpeak(int episodeId)
        {
            var storedEpisode = await _storage.GetEpisode(episodeId);
            if (storedEpisode == null)
            {
                _logger.LogWarning($"Episode {episodeId} isn't available...");
                return;
            }

            var enrichedEpisode = await _storage.GetEnrichedEpisode(episodeId);
            if (enrichedEpisode == null)
            {
                _logger.LogWarning($"Episode {episodeId} isn't enriched...");
                return;
            }

            var episodeSpeech = await _storage.GetEpisodeSpeech(episodeId);
            if (episodeSpeech != null)
            {
                _logger.LogInformation($"Episode {episodeId} already speeched...");
                return;
            }

            await GenerateSpeak(episodeId, storedEpisode, enrichedEpisode);
        }

        private async Task GenerateSpeak(int episodeId, SrStoredEpisode storedEpisode, SrStoredEnrichedEpisode enrichedEpisode)
        {
            _logger.LogInformation($"Generating speaker for episode {episodeId}...");

            var episodeSpeech = new SrStoredEpisodeSpeech();

            if (enrichedEpisode.Transcription_EN != null)
            {
                var result = await CreateAndUploadSpeech(episodeId, storedEpisode, enrichedEpisode.Transcription_EN.Text, "en-US", EnUsVoice);
                if (result != null)
                {
                    episodeSpeech.SpeechBlobIdenitifier_EN = result.Value.Key;
                    episodeSpeech.SpeechUrl_EN = result.Value.Value;
                }
            }

            if (enrichedEpisode.Transcription_SV != null)
            {
                var result = await CreateAndUploadSpeech(episodeId, storedEpisode, enrichedEpisode.Transcription_SV.Text, "sv-SE", SvSeVoice);
                if (result != null)
                {
                    episodeSpeech.SpeechBlobIdenitifier_SV = result.Value.Key;
                    episodeSpeech.SpeechUrl_SV = result.Value.Value;
                }
            }

            await _storage.StoreEpisodeSpeech(episodeId, episodeSpeech);

            _logger.LogInformation($"Generated speaker for episode {episodeId}...");
        }

        private async Task<KeyValuePair<string, string>?> CreateAndUploadSpeech(int episodeId, SrStoredEpisode storedEpisode, string text, string language, string voice)
        {
            _speechConfig.SpeechSynthesisLanguage = language;
            _speechConfig.SpeechSynthesisVoiceName = voice;
            _speechConfig.SetSpeechSynthesisOutputFormat(SpeechSynthesisOutputFormat.Audio24Khz160KBitRateMonoMp3);

            using var stream = new MemoryStream();
            using var audioStream = AudioOutputStream.CreatePushStream(new AudioPushAudioOutputStreamCallback(stream));
            using var fileOutput = AudioConfig.FromStreamOutput(audioStream);
            using var synthesizer = new SpeechSynthesizer(_speechConfig, fileOutput);

            var result = await synthesizer.SpeakTextAsync(text);

            if (result.Reason == ResultReason.SynthesizingAudioCompleted)
            {
                _logger.LogInformation($"Created speech for episode {episodeId}");
                var uploadedBlob = await UploadSpeech(storedEpisode, stream, voice);
                _logger.LogInformation($"Uploaded speech for episode {episodeId}");

                return uploadedBlob;
            }

            if (result.Reason == ResultReason.Canceled)
            {
                var cancellation = SpeechSynthesisCancellationDetails.FromResult(result);
                _logger.LogError($"Error creating speech for episode {episodeId}: Reason={cancellation.Reason}");

                if (cancellation.Reason == CancellationReason.Error)
                {
                    // Expect some texts to be to long etc
                    _logger.LogError(
                        $"Error creating speech for episode {episodeId}: ErrorCode={cancellation.ErrorCode}; ErrorDetails=[{cancellation.ErrorDetails}]");
                }

                return null;
            }

            throw new Exception($"Unknown result status for speech: {result.Reason}");
        }

        private async Task<KeyValuePair<string, string>> UploadSpeech(SrStoredEpisode storedEpisode, MemoryStream stream, string voice)
        {
            var speakerBlobIdentifier = GetBlobName(storedEpisode.Episode.Program.Id, storedEpisode.Episode, "mp3", $"Speaker_{voice}");
            var speakerBlob = _speakerContainer.GetBlockBlobReference(speakerBlobIdentifier);

            stream.Position = 0;
            speakerBlob.Properties.ContentType = "audio/mpeg";
            await speakerBlob.UploadFromStreamAsync(stream);

            return new KeyValuePair<string, string>(speakerBlobIdentifier, speakerBlob.Uri.ToString());
        }

        private static string GetBlobName(int programId, Episode episode, string extension, string type)
        {
            return $"SR/programs/{programId}/episodes/{episode.Id}/SR_{programId}__{episode.PublishDateUtc:yyyy-MM-dd}_{episode.PublishDateUtc:HH-mm}__{episode.Id}__{type}.{extension}";
        }

        private class AudioPushAudioOutputStreamCallback : PushAudioOutputStreamCallback
        {
            private readonly MemoryStream _stream;

            public AudioPushAudioOutputStreamCallback(MemoryStream stream)
            {
                _stream = stream;
            }
            public override uint Write(byte[] dataBuffer)
            {
                _stream.Write(dataBuffer, 0, dataBuffer.Length);
                return (uint)dataBuffer.Length;
            }
        }

        // Voices

        //{
        //  "Name": "Microsoft Server Speech Text to Speech Voice (en-GB, HarryNeural)",
        //  "ShortName": "en-GB-HarryNeural",
        //  "Gender": "Male",
        //  "Locale": "en-GB",
        //  "SampleRateHertz": "24000",
        //  "VoiceType": "Neural"
        //},
        //{
        //  "Name": "Microsoft Server Speech Text to Speech Voice (en-GB, MiaNeural)",
        //  "ShortName": "en-GB-MiaNeural",
        //  "Gender": "Female",
        //  "Locale": "en-GB",
        //  "SampleRateHertz": "24000",
        //  "VoiceType": "Neural"
        //},
        //{
        //  "Name": "Microsoft Server Speech Text to Speech Voice (en-US, GuyNeural)",
        //  "ShortName": "en-US-GuyNeural",
        //  "Gender": "Male",
        //  "Locale": "en-US",
        //  "SampleRateHertz": "24000",
        //  "VoiceType": "Neural"
        //},
        //{
        //  "Name": "Microsoft Server Speech Text to Speech Voice (en-US, JessaNeural)",
        //  "ShortName": "en-US-JessaNeural",
        //  "Gender": "Female",
        //  "Locale": "en-US",
        //  "SampleRateHertz": "24000",
        //  "VoiceType": "Neural"
        //},


        //{
        //  "Name": "Microsoft Server Speech Text to Speech Voice (en-US, BenjaminRUS)",
        //  "ShortName": "en-US-BenjaminRUS",
        //  "Gender": "Male",
        //  "Locale": "en-US",
        //  "SampleRateHertz": "16000",
        //  "VoiceType": "Standard"
        //},
        //{
        //  "Name": "Microsoft Server Speech Text to Speech Voice (en-US, Guy24kRUS)",
        //  "ShortName": "en-US-Guy24kRUS",
        //  "Gender": "Male",
        //  "Locale": "en-US",
        //  "SampleRateHertz": "24000",
        //  "VoiceType": "Standard"
        //},
        //{
        //  "Name": "Microsoft Server Speech Text to Speech Voice (en-US, JessaRUS)",
        //  "ShortName": "en-US-JessaRUS",
        //  "Gender": "Female",
        //  "Locale": "en-US",
        //  "SampleRateHertz": "16000",
        //  "VoiceType": "Standard"
        //},
        //{
        //  "Name": "Microsoft Server Speech Text to Speech Voice (en-US, Jessa24kRUS)",
        //  "ShortName": "en-US-Jessa24kRUS",
        //  "Gender": "Female",
        //  "Locale": "en-US",
        //  "SampleRateHertz": "24000",
        //  "VoiceType": "Standard"
        //},
        //{
        //  "Name": "Microsoft Server Speech Text to Speech Voice (en-US, ZiraRUS)",
        //  "ShortName": "en-US-ZiraRUS",
        //  "Gender": "Female",
        //  "Locale": "en-US",
        //  "SampleRateHertz": "16000",
        //  "VoiceType": "Standard"
        //},
        //{
        //  "Name": "Microsoft Server Speech Text to Speech Voice (sv-SE, HedvigRUS)",
        //  "ShortName": "sv-SE-HedvigRUS",
        //  "Gender": "Female",
        //  "Locale": "sv-SE",
        //  "SampleRateHertz": "16000",
        //  "VoiceType": "Standard"
        //},
    }
}

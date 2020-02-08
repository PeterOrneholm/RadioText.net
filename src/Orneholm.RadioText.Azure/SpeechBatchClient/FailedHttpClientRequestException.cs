using System;
using System.Net;
using System.Runtime.Serialization;

namespace Orneholm.RadioText.Azure.SpeechBatchClient
{
    [Serializable]
    public sealed class FailedHttpClientRequestException : Exception
    {
        public FailedHttpClientRequestException()
        {
            StatusCode = HttpStatusCode.Unused;
        }

        public FailedHttpClientRequestException(string message)
            : base(message)
        {
            StatusCode = HttpStatusCode.Unused;
        }

        public FailedHttpClientRequestException(string message, Exception exception)
            : base(message, exception)
        {
            StatusCode = HttpStatusCode.Unused;
        }

        public FailedHttpClientRequestException(HttpStatusCode status, string reasonPhrase)
            : base(reasonPhrase)
        {
            StatusCode = status;
        }

        private FailedHttpClientRequestException(SerializationInfo info, StreamingContext context)
            : base(info, context)
        {
            StatusCode = (HttpStatusCode)info.GetValue(nameof(this.StatusCode), typeof(HttpStatusCode));
        }

        public HttpStatusCode StatusCode { get; private set; }

        public string ReasonPhrase => Message;

        /// <inheritdoc />
        public override void GetObjectData(SerializationInfo info, StreamingContext context)
        {
            info.AddValue(nameof(StatusCode), StatusCode);

            base.GetObjectData(info, context);
        }
    }
}

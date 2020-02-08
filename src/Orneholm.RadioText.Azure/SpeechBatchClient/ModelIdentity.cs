using System;

namespace Orneholm.RadioText.Azure.SpeechBatchClient
{
    public sealed class ModelIdentity
    {
        private ModelIdentity(Guid id)
        {
            Id = id;
        }

        public Guid Id { get; }

        public static ModelIdentity Create(Guid id)
        {
            return new ModelIdentity(id);
        }
    }
}

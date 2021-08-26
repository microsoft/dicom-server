using System;

namespace Microsoft.Health.Fhir.Anonymizer.Core.Validation
{
    public class ResourceNotValidException : Exception
    {
        public ResourceNotValidException(string message) : base(message)
        {
        }
    }
}

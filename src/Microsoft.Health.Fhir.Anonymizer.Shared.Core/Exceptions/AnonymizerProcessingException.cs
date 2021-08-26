using System;

namespace Microsoft.Health.Fhir.Anonymizer.Core.Exceptions
{
    // Processing exception. A runtime exception thrown during anonymization process.
    // Customers can set the parameter in configuration file to skip processing the resource if this exception is thrown.
    public class AnonymizerProcessingException : Exception
    {
        public AnonymizerProcessingException(string message) : base(message)
        {
        }

        public AnonymizerProcessingException(string message, Exception innerException) : base(message, innerException)
        {
        }
    }
}

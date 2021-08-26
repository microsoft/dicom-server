using Microsoft.Extensions.Logging;

namespace Microsoft.Health.Fhir.Anonymizer.Core
{
    public static class AnonymizerLogging
    {
        public static ILoggerFactory LoggerFactory { get; set; } = new LoggerFactory();
        public static ILogger CreateLogger<T>() => LoggerFactory.CreateLogger<T>();
    }
}

namespace Microsoft.Health.Fhir.Anonymizer.Core.AnonymizerConfigurations
{
    public enum ProcessingErrorsOption
    {
        Raise, // Invalid processing will raise an exception.
        Skip,  // Invalid processing will return empty element.
        // Ignore Invalid processing will return input.
    }
}

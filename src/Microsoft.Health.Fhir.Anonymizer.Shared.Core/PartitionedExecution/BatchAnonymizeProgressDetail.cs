namespace Microsoft.Health.Fhir.Anonymizer.Core.PartitionedExecution
{
    public class BatchAnonymizeProgressDetail
    {
        public int CurrentThreadId { get; set; }

        // The number of anonymization completed resources.
        public int ProcessCompleted { get; set; }

        // The number of skipped resources when skipping AnonymizerProcessingException enabled.
        public int ProcessSkipped { get; set; }

        // Todo : this property will be removed since exception will be thrown once consuming failed.
        public int ConsumeCompleted { get; set; }
    }
}

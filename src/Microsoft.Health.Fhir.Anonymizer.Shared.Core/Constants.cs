using System.Collections.Generic;

namespace Microsoft.Health.Fhir.Anonymizer.Core
{
    internal static partial class Constants
    {
        // InstanceType constants
        internal const string DateTypeName = "date";
        internal const string DateTimeTypeName = "dateTime";
        internal const string DecimalTypeName = "decimal";
        internal const string InstantTypeName = "instant";
        internal const string AgeTypeName = "Age";
        internal const string BundleTypeName = "Bundle";
        internal const string ReferenceTypeName = "Reference";

        // NodeName constants
        internal const string PostalCodeNodeName = "postalCode";
        internal const string ReferenceStringNodeName = "reference";
        internal const string ContainedNodeName = "contained";
        internal const string EntryNodeName = "entry";
        internal const string EntryResourceNodeName = "resource";
        internal const string ValueNodeName = "value";

        // Rule constants
        internal const string PathKey = "path";
        internal const string MethodKey = "method";

        internal const int DefaultPartitionedExecutionCount = 4;
        internal const int DefaultPartitionedExecutionBatchSize = 1000;

        internal const string GeneralResourceType = "Resource";
        internal const string GeneralDomainResourceType = "DomainResource";

    }
}

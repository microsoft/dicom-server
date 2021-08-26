using System.Collections.Generic;
using Hl7.Fhir.Model;

namespace Microsoft.Health.Fhir.Anonymizer.Core.Processors
{
    public partial class PerturbProcessor : IAnonymizerProcessor
    {
#pragma warning disable IDE1006 // Naming Styles
        private static readonly HashSet<string> s_quantityTypeNames = new HashSet<string>
#pragma warning restore IDE1006 // Naming Styles
        {
            FHIRAllTypes.Age.ToString(),
            FHIRAllTypes.Count.ToString(),
            FHIRAllTypes.Duration.ToString(),
            FHIRAllTypes.Distance.ToString(),
            FHIRAllTypes.Money.ToString(),
            FHIRAllTypes.MoneyQuantity.ToString(),
            FHIRAllTypes.Quantity.ToString(),
            FHIRAllTypes.SimpleQuantity.ToString()
        };
    }
}

using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using Hl7.Fhir.Model;
using Hl7.Fhir.Validation;

namespace Microsoft.Health.Fhir.Anonymizer.Core.Validation
{
    public class AttributeValidator
    {
        public IEnumerable<ValidationResult> Validate(Resource resource)
        {
            var result = new List<ValidationResult>();
            DotNetAttributeValidation.TryValidate(resource, result, true);
            return result;
        }
    }
}

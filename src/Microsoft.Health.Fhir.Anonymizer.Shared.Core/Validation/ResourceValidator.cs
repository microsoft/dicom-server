using System.Linq;
using Hl7.Fhir.Model;
using Microsoft.Extensions.Logging;

namespace Microsoft.Health.Fhir.Anonymizer.Core.Validation
{
    public class ResourceValidator
    {
        private readonly AttributeValidator _validator = new AttributeValidator();
        private readonly ILogger _logger = AnonymizerLogging.CreateLogger<ResourceValidator>();

        public void ValidateInput(Resource resource)
        {
            var results = _validator.Validate(resource);
            foreach (var error in results)
            {
                var path = string.IsNullOrEmpty(error.MemberNames?.FirstOrDefault()) ? string.Empty : error.MemberNames?.FirstOrDefault();
                _logger.LogDebug(string.IsNullOrEmpty(resource?.Id) ?
                    $"The input is non-conformant with FHIR specification: {error.ErrorMessage} for {path} in {resource.TypeName}." :
                    $"The input of resource ID {resource.Id} is non-conformant with FHIR specification: {error.ErrorMessage} for {path} in {resource.TypeName}.");
            }

            if (results.Any())
            {
                throw new ResourceNotValidException("The input is non-conformant with FHIR specification. Please open verbose log for more details. (-v)");
            }
        }

        public void ValidateOutput(Resource resource)
        {
            var results = _validator.Validate(resource);
            foreach (var error in results)
            {
                var path = error.MemberNames?.FirstOrDefault() ?? string.Empty;
                _logger.LogDebug(string.IsNullOrEmpty(resource?.Id) ?
                    $"The output is non-conformant with FHIR specification: {error.ErrorMessage} for {path} in {resource.TypeName}." :
                    $"The output of resource ID {resource.Id} is non-conformant with FHIR specification: {error.ErrorMessage} for {path} in {resource.TypeName}.");
            }

            if (results.Any())
            {
                throw new ResourceNotValidException("The output is non-conformant with FHIR specification. Please open verbose log for more details. (-v)");
            }
        }
    }
}

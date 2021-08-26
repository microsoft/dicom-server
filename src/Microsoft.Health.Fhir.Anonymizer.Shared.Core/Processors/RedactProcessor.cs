using System.Collections.Generic;
using Hl7.Fhir.ElementModel;
using Microsoft.Health.Fhir.Anonymizer.Core.AnonymizerConfigurations;
using Microsoft.Health.Fhir.Anonymizer.Core.Extensions;
using Microsoft.Health.Fhir.Anonymizer.Core.Models;
using Microsoft.Health.Fhir.Anonymizer.Core.Utility;

namespace Microsoft.Health.Fhir.Anonymizer.Core.Processors
{
    public class RedactProcessor : IAnonymizerProcessor
    {
        public bool EnablePartialDatesForRedact { get; set; } = false;

        public bool EnablePartialAgesForRedact { get; set; } = false;

        public bool EnablePartialZipCodesForRedact { get; set; } = false;

        public List<string> RestrictedZipCodeTabulationAreas { get; set; } = null;

        public RedactProcessor(bool enablePartialDatesForRedact, bool enablePartialAgesForRedact, bool enablePartialZipCodesForRedact, List<string> restrictedZipCodeTabulationAreas)
        {
            EnablePartialDatesForRedact = enablePartialDatesForRedact;
            EnablePartialAgesForRedact = enablePartialAgesForRedact;
            EnablePartialZipCodesForRedact = enablePartialZipCodesForRedact;
            RestrictedZipCodeTabulationAreas = restrictedZipCodeTabulationAreas;
        }

        public static RedactProcessor Create(AnonymizerConfigurationManager configuratonManager)
        {
            var parameters = configuratonManager.GetParameterConfiguration();
            return new RedactProcessor(parameters.EnablePartialDatesForRedact, parameters.EnablePartialAgesForRedact,
                parameters.EnablePartialZipCodesForRedact, parameters.RestrictedZipCodeTabulationAreas);
        }

        public ProcessResult Process(ElementNode node, ProcessContext context = null, Dictionary<string, object> settings = null)
        {
            if (string.IsNullOrEmpty(node?.Value?.ToString()))
            {
                return new ProcessResult();
            }

            if (node.IsDateNode())
            {
                return DateTimeUtility.RedactDateNode(node, EnablePartialDatesForRedact);
            }
            else if (node.IsDateTimeNode() || node.IsInstantNode())
            {
                return DateTimeUtility.RedactDateTimeAndInstantNode(node, EnablePartialDatesForRedact);
            }
            else if (node.IsAgeDecimalNode())
            {
                return DateTimeUtility.RedactAgeDecimalNode(node, EnablePartialAgesForRedact);
            }
            else if (node.IsPostalCodeNode())
            {
                return PostalCodeUtility.RedactPostalCode(node, EnablePartialZipCodesForRedact, RestrictedZipCodeTabulationAreas);
            }
            else
            {
                node.Value = null;
                ProcessResult result = new ProcessResult();
                result.AddProcessRecord(AnonymizationOperations.Redact, node);
                return result;
            }
        }
    }
}

using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using Hl7.Fhir.ElementModel;
using Microsoft.Health.Fhir.Anonymizer.Core.Extensions;
using Microsoft.Health.Fhir.Anonymizer.Core.Models;
using Microsoft.Health.Fhir.Anonymizer.Core.Processors;

namespace Microsoft.Health.Fhir.Anonymizer.Core.Utility
{
    public class PostalCodeUtility
    {
        private static readonly string s_replacementDigit = "0";
        private static readonly int s_initialDigitsCount = 3;

        public static ProcessResult RedactPostalCode(ElementNode node, bool enablePartialZipCodesForRedact = false, List<string> restrictedZipCodeTabulationAreas = null)
        {
            var processResult = new ProcessResult();
            if (!node.IsPostalCodeNode() || string.IsNullOrEmpty(node?.Value?.ToString()))
            {
                return processResult;
            }

            if (enablePartialZipCodesForRedact)
            {
                if (restrictedZipCodeTabulationAreas != null && restrictedZipCodeTabulationAreas.Any(x => node.Value.ToString().StartsWith(x)))
                {
                    node.Value = Regex.Replace(node.Value.ToString(), @"\d", s_replacementDigit);
                }
                else if (node.Value.ToString().Length >= s_initialDigitsCount)
                {
                    var suffix = node.Value.ToString().Substring(s_initialDigitsCount);
                    node.Value = $"{node.Value.ToString().Substring(0, s_initialDigitsCount)}{Regex.Replace(suffix, @"\d", s_replacementDigit)}";
                }
                processResult.AddProcessRecord(AnonymizationOperations.Abstract, node);
            }
            else
            {
                node.Value = null;
                processResult.AddProcessRecord(AnonymizationOperations.Redact, node);
            }

            return processResult;
        }
    }
}

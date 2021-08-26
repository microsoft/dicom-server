using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using Hl7.Fhir.Model;

namespace Microsoft.Health.Fhir.Anonymizer.Core.Utility
{
    public class ReferenceUtility
    {
        private const string InternalReferencePrefix = "#";
        private static readonly List<Regex> _literalReferenceRegexes = new List<Regex>
        {
            // Regex for absolute or relative url reference, https://www.hl7.org/fhir/references.html#literal
            new Regex(@"^(?<prefix>((http|https)://([A-Za-z0-9\\\/\.\:\%\$])*)?("
                + String.Join("|", ModelInfo.SupportedResources)
                + @")\/)(?<id>[A-Za-z0-9\-\.]{1,64})(?<suffix>\/_history\/[A-Za-z0-9\-\.]{1,64})?$"),
            // Regex for oid reference https://www.hl7.org/fhir/datatypes.html#oid
            new Regex(@"^(?<prefix>urn:oid:)(?<id>[0-2](\.(0|[1-9][0-9]*))+)(?<suffix>)$"),
            // Regex for uuid reference https://www.hl7.org/fhir/datatypes.html#uuid
            new Regex(@"^(?<prefix>urn:uuid:)(?<id>[0-9a-fA-F]{8}-[0-9a-fA-F]{4}-[0-9a-fA-F]{4}-[0-9a-fA-F]{4}-[0-9a-fA-F]{12})(?<suffix>)$")
        };

        public static string TransformReferenceId(string reference, Func<string, string> transformation)
        {
            if (string.IsNullOrEmpty(reference))
            {
                return reference;
            }

            if (reference.StartsWith(InternalReferencePrefix))
            {
                var internalId = reference.Substring(InternalReferencePrefix.Length);
                var newReference = $"{InternalReferencePrefix}{transformation(internalId)}";

                return newReference;
            }

            foreach (var regex in _literalReferenceRegexes)
            {
                var match = regex.Match(reference);
                if (match.Success)
                {
                    var group = match.Groups["id"];
                    var newId = transformation(group.Value);
                    var newReference = $"{match.Groups["prefix"].Value}{newId}{match.Groups["suffix"].Value}";

                    return newReference;
                }
            }

            // No id pattern found in reference, will hash whole reference value
            return transformation(reference);
        }
    }
}

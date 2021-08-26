using Hl7.Fhir.ElementModel;
using Hl7.Fhir.Model;
using Hl7.Fhir.Serialization;
using Hl7.Fhir.Specification;
using Hl7.Fhir.Utility;
using Microsoft.Health.Fhir.Anonymizer.Core.Models;
using System.Collections.Generic;
using System.Linq;

namespace Microsoft.Health.Fhir.Anonymizer.Core.Models
{
    /// <summary>
    /// Empty element format example:
    /// {
    ///     "resourceType": "Patient",
    ///     "meta": {
    ///            "security": [
    ///                  {
    ///            "system": "http://terminology.hl7.org/CodeSystem/v3-ObservationValue",
    ///            "code": "REDACTED",
    ///            "display": "redacted"
    ///        }
    ///    ]
    ///}
    /// </summary>
    public class EmptyElement : ITypedElement
    {
        public EmptyElement(string instanceType)
        {
            InstanceType = instanceType;
        }

        private static Meta _meta = new Meta() { Security = new List<Coding>() { SecurityLabels.REDACT } };

        private static IStructureDefinitionSummaryProvider _provider = new PocoStructureDefinitionSummaryProvider();

        private static FhirJsonParser _parser = new FhirJsonParser();

#pragma warning disable CA1051 // Do not declare visible instance fields
        protected List<ITypedElement> ChildList = new List<ITypedElement>() { _meta.ToTypedElement("meta") };
#pragma warning restore CA1051 // Do not declare visible instance fields

        public string Name => "empty";

        public string InstanceType { get; set; }

        public object Value => null;

        public string Location => Name;

        public IElementDefinitionSummary Definition => ElementDefinitionSummary.ForRoot(_provider.Provide(InstanceType));

#pragma warning disable CS8632 // The annotation for nullable reference types should only be used in code within a '#nullable' annotations context.
        public IEnumerable<ITypedElement> Children(string? name = null) =>
#pragma warning restore CS8632 // The annotation for nullable reference types should only be used in code within a '#nullable' annotations context.
            name == null ? ChildList : ChildList.Where(c => c.Name.MatchesPrefix(name));

        public static bool IsEmptyElement(ITypedElement element)
        {
            if (element is EmptyElement)
            {
                return true;
            }

            return element.Children().Count() == 1 && element.Children("meta").Count() == 1;
        }

        public static bool IsEmptyElement(string elementJson)
        {
            try
            {
                var element = _parser.Parse(elementJson).ToTypedElement();
                return IsEmptyElement(element);
            }
            catch
            {
                return false;
            }
        }

        public static bool IsEmptyElement(object element)
        {
            if (element is string)
            {
                return IsEmptyElement(element as string);
            }
            else if (element is ITypedElement)
            {
                return IsEmptyElement(element as ITypedElement);
            }

            return false;
        }
    }
}

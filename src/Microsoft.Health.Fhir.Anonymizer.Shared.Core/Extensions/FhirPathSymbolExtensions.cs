using System.Collections.Generic;
using System.Linq;
using Microsoft.Health.Fhir.Anonymizer.Core.Extensions;
using Hl7.Fhir.ElementModel;
using Hl7.FhirPath.Expressions;

namespace Microsoft.Health.Fhir.Anonymizer.Core.Extensions
{
    public static class FhirPathSymbolExtensions
    {
        private static object _lock = new object();
        public static SymbolTable AddExtensionSymbols(this SymbolTable t)
        {
            // Add lock here to ensure thread safety when modifying a symbol table
            lock (_lock)
            {
                // Check whether extension method already exists
                if (t.Filter("nodesByType", 2).Count() == 0)
                {
                    t.Add("nodesByType", (IEnumerable<ITypedElement> f, string typeName) => NodesByType(f, typeName), doNullProp: true);
                }

                if (t.Filter("nodesByName", 2).Count() == 0)
                {
                    t.Add("nodesByName", (IEnumerable<ITypedElement> f, string name) => NodesByName(f, name), doNullProp: true);
                }
            }

            return t;
        }

        public static IEnumerable<ITypedElement> NodesByType(IEnumerable<ITypedElement> nodes, string typeName)
        {
            return nodes.CastElementNodes().SelfAndDescendantsWithoutSubResource().Where(n => typeName.Equals(n.InstanceType));
        }

        public static IEnumerable<ITypedElement> NodesByName(IEnumerable<ITypedElement> nodes, string name)
        {
            return nodes.CastElementNodes().SelfAndDescendantsWithoutSubResource().Where(n => name.Equals(n.Name));
        }
    }
}

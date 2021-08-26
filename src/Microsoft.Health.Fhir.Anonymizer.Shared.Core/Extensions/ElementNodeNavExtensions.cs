using System.Collections.Generic;
using System.Linq;
using Microsoft.Health.Fhir.Anonymizer.Core.Extensions;
using Hl7.Fhir.ElementModel;
using Hl7.FhirPath;

namespace Microsoft.Health.Fhir.Anonymizer.Core.Extensions
{
    public static class ElementNodeNavExtensions
    {
        public static List<ElementNode> GetEntryResourceChildren(this ElementNode node)
        {
            return node?.Children(Constants.EntryNodeName)
                    .Select(entry => entry?.Children(Constants.EntryResourceNodeName).FirstOrDefault())
                    .Where(resource => resource != null)
                    .CastElementNodes()
                    .ToList();
        }

        public static List<ElementNode> GetContainedChildren(this ElementNode node)
        {
            return node?.Children(Constants.ContainedNodeName).CastElementNodes().ToList();
        }

        public static IEnumerable<ElementNode> ResourceDescendantsWithoutSubResource(this ElementNode node)
        {
            foreach (var child in node.Children().CastElementNodes())
            {
                // Skip sub resources in bundle entry and contained list
                if (child.IsFhirResource())
                {
                    continue;
                }

                yield return child;

                foreach (var n in child.ResourceDescendantsWithoutSubResource())
                {
                    yield return n;
                }
            }
        }

        public static IEnumerable<ElementNode> SelfAndDescendantsWithoutSubResource(this IEnumerable<ElementNode> nodes)
        {
            foreach (var node in nodes)
            {
                yield return node;

                foreach (var descendant in node.ResourceDescendantsWithoutSubResource())
                {
                    yield return descendant;
                }
            }
        }
    }
}

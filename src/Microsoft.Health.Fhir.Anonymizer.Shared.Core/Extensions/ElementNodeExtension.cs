using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using Hl7.Fhir.ElementModel;

namespace Microsoft.Health.Fhir.Anonymizer.Core.Extensions
{
    public static class ElementNodeExtension
    {

        private static readonly string s_locationToFhirPathRegex = @"\[.*?\]";

        public static bool IsDateNode(this ElementNode node)
        {
            return node != null && string.Equals(node.InstanceType, Constants.DateTypeName, StringComparison.OrdinalIgnoreCase);
        }

        public static bool IsDateTimeNode(this ElementNode node)
        {
            return node != null && string.Equals(node.InstanceType, Constants.DateTimeTypeName, StringComparison.OrdinalIgnoreCase);
        }

        public static bool IsAgeDecimalNode(this ElementNode node)
        {
            return node != null &&
                node.Parent.IsAgeNode() &&
                string.Equals(node.InstanceType, Constants.DecimalTypeName, StringComparison.OrdinalIgnoreCase);
        }

        public static bool IsInstantNode(this ElementNode node)
        {
            return node != null && string.Equals(node.InstanceType, Constants.InstantTypeName, StringComparison.OrdinalIgnoreCase);
        }

        public static bool IsAgeNode(this ElementNode node)
        {
            return node != null && string.Equals(node.InstanceType, Constants.AgeTypeName, StringComparison.OrdinalIgnoreCase);
        }

        public static bool IsBundleNode(this ElementNode node)
        {
            return node != null && string.Equals(node.InstanceType, Constants.BundleTypeName, StringComparison.OrdinalIgnoreCase);
        }

        public static bool IsReferenceNode(this ElementNode node)
        {
            return node != null && string.Equals(node.InstanceType, Constants.ReferenceTypeName, StringComparison.OrdinalIgnoreCase);
        }

        public static bool IsPostalCodeNode(this ElementNode node)
        {
            return node != null && string.Equals(node.Name, Constants.PostalCodeNodeName, StringComparison.OrdinalIgnoreCase);
        }

        public static bool IsReferenceStringNode(this ElementNode node)
        {
            return node != null &&
                node.Parent.IsReferenceNode() &&
                string.Equals(node.Name, Constants.ReferenceStringNodeName, StringComparison.OrdinalIgnoreCase);
        }

        public static bool IsEntryNode(this ElementNode node)
        {
            return node != null && string.Equals(node.Name, Constants.EntryNodeName, StringComparison.OrdinalIgnoreCase);
        }

        public static bool IsContainedNode(this ElementNode node)
        {
            return node != null && string.Equals(node.Name, Constants.ContainedNodeName, StringComparison.OrdinalIgnoreCase);
        }

        public static bool HasContainedNode(this ElementNode node)
        {
            return node != null && node.Children(Constants.ContainedNodeName).Any();
        }

        public static bool IsFhirResource(this ElementNode node)
        {
            return node != null && (node.Definition?.IsResource ?? false);
        }

        public static string GetFhirPath(this ElementNode node)
        {
            return node == null ? string.Empty : Regex.Replace(node.Location, s_locationToFhirPathRegex, string.Empty);
        }

        public static string GetNodeId(this ElementNode node)
        {
            var id = node.Children("id").FirstOrDefault();
            return id?.Value?.ToString() ?? string.Empty;
        }

        public static ElementNode GetMeta(this ElementNode node)
        {
            return node?.Children("meta").CastElementNodes().FirstOrDefault();
        }

        public static IEnumerable<ElementNode> CastElementNodes(this IEnumerable<ITypedElement> input)
        {
            return input.Select(ToElement).Cast<ElementNode>();
        }

        private static ITypedElement ToElement(ITypedElement node)
        {
            return node is ScopedNode scopedNode
                ? scopedNode.Current
                : node;
        }
    }
}

using System.Collections.Generic;
using System.Linq;
using EnsureThat;
using Microsoft.Health.Fhir.Anonymizer.Core.Extensions;
using Hl7.Fhir.ElementModel;
using Hl7.Fhir.Model;
using Hl7.Fhir.Rest;
using Hl7.Fhir.Serialization;
using Hl7.Fhir.Specification;
using Hl7.FhirPath.Sprache;
using Microsoft.Health.Fhir.Anonymizer.Core.Exceptions;
using Microsoft.Health.Fhir.Anonymizer.Core.Models;
using Microsoft.Health.Fhir.Anonymizer.Core.Processors.Settings;

namespace Microsoft.Health.Fhir.Anonymizer.Core.Processors
{
    public class SubstituteProcessor : IAnonymizerProcessor
    {
        private readonly FhirJsonParser _parser = new FhirJsonParser();
        private static readonly PocoStructureDefinitionSummaryProvider s_provider = new PocoStructureDefinitionSummaryProvider();

        public ProcessResult Process(ElementNode node, ProcessContext context = null, Dictionary<string, object> settings = null)
        {
            EnsureArg.IsNotNull(node);
            EnsureArg.IsNotNull(context?.VisitedNodes);
            EnsureArg.IsNotNull(settings);

            var substituteSetting = SubstituteSetting.CreateFromRuleSettings(settings);
            ElementNode replacementNode;
            // Get replacementNode for substitution  
            if (ModelInfo.IsPrimitive(node.InstanceType))
            {
                // Handle replaceWith value of string
                replacementNode = GetPrimitiveNode(substituteSetting.ReplaceWith);
            }
            else
            {
                // Handle replaceWith value of json object
                var replacementNodeType = ModelInfo.GetTypeForFhirType(node.InstanceType);
                if (replacementNodeType == null)
                {
                    // Shall never throws here
                    throw new AnonymizerProcessingException($"Node type is invalid at path {node.GetFhirPath()}.");
                }
                // Convert null object to empty object
                var replaceWith = substituteSetting.ReplaceWith ?? "{}";
                var replaceElement = _parser.Parse(replaceWith, replacementNodeType).ToTypedElement();
                replacementNode = ElementNode.FromElement(replaceElement);
            }

            var keepNodes = new HashSet<ElementNode>();
            // Retrieve all nodes that have been processed before to keep 
            _ = GenerateKeepNodeSetForSubstitution(node, context.VisitedNodes, keepNodes);
            var processResult = SubstituteNode(node, replacementNode, context.VisitedNodes, keepNodes);
            MarkSubstitutedFragmentAsVisited(node, context.VisitedNodes);

            return processResult;
        }

        private ProcessResult SubstituteNode(ElementNode node, ElementNode replacementNode, HashSet<ElementNode> visitedNodes, HashSet<ElementNode> keepNodes)
        {
            var processResult = new ProcessResult();
            if (node == null || replacementNode == null || visitedNodes.Contains(node))
            {
                return processResult;
            }

            // children names to replace, multiple to multiple replacement
            var replaceChildrenNames = replacementNode.Children().Select(element => element.Name).ToHashSet();
            foreach (var name in replaceChildrenNames)
            {
                var children = node.Children(name).CastElementNodes().ToList();
                var targetChildren = replacementNode.Children(name).CastElementNodes().ToList();

                int i = 0;
                foreach (var child in children)
                {
                    if (visitedNodes.Contains(child))
                    {
                        // Skip replacement if child already processed before.
                        i++;
                        continue;
                    }
                    else if (i < targetChildren.Count)
                    {
                        // We still have target nodes, do replacement
                        SubstituteNode(child, targetChildren[i++], visitedNodes, keepNodes);
                    }
                    else if (keepNodes.Contains(child))
                    {
                        // Substitute with an empty node when no target node available but we need to keep this node
                        SubstituteNode(child, GetDummyNode(), visitedNodes, keepNodes);
                    }
                    else
                    {
                        // Remove source node when no target node available and we don't need to keep the source node
                        node.Remove(child);
                    }
                }

                while (i < targetChildren.Count)
                {
                    // Add extra target nodes, create a new copy before adding
                    node.Add(s_provider, ElementNode.FromElement(targetChildren[i++]));
                }
            }

            // children nodes not presented in replacement value, we need either remove or keep a dummy copy
            var nonReplacementChildren = node.Children()
                .Where(element => !replaceChildrenNames.Contains(element.Name))
                .CastElementNodes().ToList();
            foreach (var child in nonReplacementChildren)
            {
                if (visitedNodes.Contains(child))
                {
                    continue;
                }
                else if (keepNodes.Contains(child))
                {
                    SubstituteNode(child, GetDummyNode(), visitedNodes, keepNodes);
                }
                else
                {
                    node.Remove(child);
                }
            }

            node.Value = replacementNode.Value;
            processResult.AddProcessRecord(AnonymizationOperations.Substitute, node);
            return processResult;
        }

        // To keep consistent anonymization changes made by preceding rules, we should figure out whether a node can be removed during substitution
        private bool GenerateKeepNodeSetForSubstitution(ElementNode node, HashSet<ElementNode> visitedNodes, HashSet<ElementNode> keepNodes)
        {
            var shouldKeep = false;
            // If a child (no matter how deep) has been modified, this node should be kept
            foreach (var child in node.Children().CastElementNodes())
            {
                shouldKeep |= GenerateKeepNodeSetForSubstitution(child, visitedNodes, keepNodes);
            }

            // If this node its self has been modified, it should be kept
            if (shouldKeep || visitedNodes.Contains(node))
            {
                keepNodes.Add(node);
                return true;
            }

            return shouldKeep;
        }

        // Post-process to mark all substituted children nodes as visited
        private void MarkSubstitutedFragmentAsVisited(ElementNode node, HashSet<ElementNode> visitedNodes)
        {
            visitedNodes.Add(node);
            foreach (var child in node.Children().CastElementNodes())
            {
                MarkSubstitutedFragmentAsVisited(child, visitedNodes);
            }
        }

        private ElementNode GetPrimitiveNode(string value)
        {
            var node = ElementNode.FromElement(ElementNode.ForPrimitive(value ?? string.Empty));
            if (value == null)
            {
                // Set empty node value to null to ensure a correct serialization result
                node.Value = null;
            }
            return node;
        }

        private ElementNode GetDummyNode()
        {
            var dummy = GetPrimitiveNode(null);
            return dummy;
        }
    }
}

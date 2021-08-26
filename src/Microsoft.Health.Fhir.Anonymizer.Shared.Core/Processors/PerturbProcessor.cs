using System;
using System.Collections.Generic;
using System.Linq;
using EnsureThat;
using Microsoft.Health.Fhir.Anonymizer.Core.Extensions;
using Hl7.Fhir.ElementModel;
using Hl7.Fhir.Model;
using MathNet.Numerics.Distributions;
using Microsoft.Health.Fhir.Anonymizer.Core.Models;
using Microsoft.Health.Fhir.Anonymizer.Core.Processors.Settings;

namespace Microsoft.Health.Fhir.Anonymizer.Core.Processors
{
    public partial class PerturbProcessor : IAnonymizerProcessor
    {
        private static readonly HashSet<string> s_primitiveValueTypeNames = new HashSet<string>
        {
            FHIRAllTypes.Decimal.ToString(),
            FHIRAllTypes.Integer.ToString(),
            FHIRAllTypes.PositiveInt.ToString(),
            FHIRAllTypes.UnsignedInt.ToString()
        };

        private static readonly HashSet<string> s_integerValueTypeNames = new HashSet<string>
        {
            FHIRAllTypes.Integer.ToString(),
            FHIRAllTypes.PositiveInt.ToString(),
            FHIRAllTypes.UnsignedInt.ToString()
        };

        public ProcessResult Process(ElementNode node, ProcessContext context = null, Dictionary<string, object> settings = null)
        {
            EnsureArg.IsNotNull(node);
            EnsureArg.IsNotNull(context?.VisitedNodes);
            EnsureArg.IsNotNull(settings);

            var result = new ProcessResult();

            ElementNode valueNode = null;
            if (s_primitiveValueTypeNames.Contains(node.InstanceType, StringComparer.InvariantCultureIgnoreCase))
            {
                valueNode = node;
            }
            else if (s_quantityTypeNames.Contains(node.InstanceType, StringComparer.InvariantCultureIgnoreCase))
            {
                valueNode = node.Children(Constants.ValueNodeName).CastElementNodes().FirstOrDefault();
            }

            // Perturb will not happen if value node is empty or visited.
            if (valueNode?.Value == null || context.VisitedNodes.Contains(valueNode))
            {
                return result;
            }

            var perturbSetting = PerturbSetting.CreateFromRuleSettings(settings);

            AddNoise(valueNode, perturbSetting);
            context.VisitedNodes.UnionWith(node.Descendants().CastElementNodes());
            result.AddProcessRecord(AnonymizationOperations.Perturb, node);
            return result;
        }

        private void AddNoise(ElementNode node, PerturbSetting perturbSetting)
        {
            if (s_integerValueTypeNames.Contains(node.InstanceType, StringComparer.InvariantCultureIgnoreCase))
            {
                perturbSetting.RoundTo = 0;
            }

            var originValue = decimal.Parse(node.Value.ToString());
            var span = perturbSetting.Span;
            if (perturbSetting.RangeType == PerturbRangeType.Proportional)
            {
                span = (double)originValue * perturbSetting.Span;
            }

            var noise = (decimal)ContinuousUniform.Sample(-1 * span / 2, span / 2);
            var perturbedValue = decimal.Round(originValue + noise, perturbSetting.RoundTo);
            if (perturbedValue <= 0 && string.Equals(FHIRAllTypes.PositiveInt.ToString(), node.InstanceType, StringComparison.OrdinalIgnoreCase))
            {
                perturbedValue = 1;
            }
            if (perturbedValue < 0 && string.Equals(FHIRAllTypes.UnsignedInt.ToString(), node.InstanceType, StringComparison.OrdinalIgnoreCase))
            {
                perturbedValue = 0;
            }
            node.Value = perturbedValue;
            return;
        }
    }
}

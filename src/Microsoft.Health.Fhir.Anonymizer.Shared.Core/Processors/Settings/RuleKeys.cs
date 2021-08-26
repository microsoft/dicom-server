namespace Microsoft.Health.Fhir.Anonymizer.Core.Processors.Settings
{
    internal static class RuleKeys
    {
        //perturb
        internal const string ReplaceWith = "replaceWith";
        internal const string RangeType = "rangeType";
        internal const string RoundTo = "roundTo";
        internal const string Span = "span";

        //generalize
        internal const string Cases = "cases";
        internal const string OtherValues = "otherValues";
    }
}

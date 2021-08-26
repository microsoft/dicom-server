using Hl7.Fhir.ElementModel;

namespace Microsoft.Health.Fhir.Anonymizer.Core.Visitors
{
    public abstract class AbstractElementNodeVisitor
    {
        public virtual bool Visit(ElementNode node)
        {
            return true;
        }

        public virtual void EndVisit(ElementNode node)
        {

        }
    }
}

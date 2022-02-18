using Cybersource.Models;
using GraphQL;
using GraphQL.Types;

namespace Cybersource.GraphQL.Types
{
    [GraphQLMetadata("ConversionDetail")]
    public class ConversionDetailType : ObjectGraphType<ConversionDetail>
    {
        public ConversionDetailType()
        {
            Name = "ConversionDetail";
            Field(d => d.ConversionTime);
            Field(d => d.MerchantReferenceNumber);
            Field(d => d.NewDecision);
            Field(d => d.OriginalDecision);
            Field(d => d.Reviewer);
            Field(d => d.ReviewerComments);
        }
    }
}

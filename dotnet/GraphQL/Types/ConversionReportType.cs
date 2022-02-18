using Cybersource.Models;
using GraphQL;
using GraphQL.Types;

namespace Cybersource.GraphQL.Types
{
    [GraphQLMetadata("ConversionReport")]
    public class ConversionReportType : ObjectGraphType<ConversionReportResponse>
    {
        public ConversionReportType()
        {
            Name = "ConversionReport";
            Field(r => r.ConversionDetails, type: typeof(ListGraphType<ConversionDetailType>)).Description("List of Order Conversions.");
        }
    }
}

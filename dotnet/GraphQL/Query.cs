using Cybersource.Data;
using Cybersource.GraphQL.Types;
using Cybersource.Models;
using Cybersource.Services;
using GraphQL;
using GraphQL.Types;
using System;
using System.Collections.Generic;

namespace Cybersource.GraphQL
{
    [GraphQLMetadata("Query")]
    public class Query : ObjectGraphType<object>
    {
        public Query(ICybersourcePaymentService cybersourcePaymentService, ICybersourceRepository cybersourceRepository)
        {
            Name = "Query";

            FieldAsync<ConversionReportType>(
                "conversionReport",
                arguments: new QueryArguments(
                    new QueryArgument<StringGraphType> { Name = "startDate", Description = "Start Date" },
                    new QueryArgument<StringGraphType> { Name = "endDate", Description = "End Date" }
                ),
                resolve: async context =>
                {

                    string startDate = context.GetArgument<string>("startDate");
                    string endDate = context.GetArgument<string>("endDate");
                    if(string.IsNullOrEmpty(startDate) && string.IsNullOrEmpty(endDate))
                    {
                        startDate = DateTime.Now.AddDays(-1).ToString();
                        endDate = DateTime.Now.ToString();
                    }

                    ConversionReportResponse conversionReport = await cybersourcePaymentService.ConversionDetailReport(startDate, endDate);

                    return conversionReport;
                }
            );
            
            FieldAsync<ListGraphType<StringGraphType>>(
                "merchantDefinedFields",
                resolve: async context =>
                {
                    PaymentRequestWrapper requestWrapper = new PaymentRequestWrapper(new CreatePaymentRequest());
                    return requestWrapper.GetPropertyList();
                }
            );
        }
    }
}

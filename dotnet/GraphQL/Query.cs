﻿using Cybersource.Services;
using GraphQL;
using GraphQL.Types;
using System;
using Cybersource.Models;
using cybersource.GraphQL.Types;

namespace Cybersource.GraphQL
{
    [GraphQLMetadata("Query")]
    public class Query : ObjectGraphType<object>
    {
        public Query(ICybersourcePaymentService cybersourcePaymentService)
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
                    if(string.IsNullOrEmpty(startDate) && string.IsNullOrEmpty(startDate))
                    {
                        startDate = DateTime.Now.AddDays(-1).ToString();
                        endDate = DateTime.Now.ToString();
                    }

                    ConversionReportResponse conversionReport = await cybersourcePaymentService.ConversionDetailReport(startDate, endDate);

                    return conversionReport;
                }
            );
        }
    }
}

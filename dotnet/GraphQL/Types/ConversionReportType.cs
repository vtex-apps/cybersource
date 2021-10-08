﻿using Cybersource.Models;
using GraphQL;
using GraphQL.Types;
using System;
using System.Collections.Generic;
using System.Text;

namespace cybersource.GraphQL.Types
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

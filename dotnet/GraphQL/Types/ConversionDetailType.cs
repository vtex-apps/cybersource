﻿using Cybersource.Models;
using GraphQL;
using GraphQL.Types;
using System;
using System.Collections.Generic;
using System.Text;

namespace cybersource.GraphQL.Types
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

using Cybersource.Services;
using GraphQL;
using GraphQL.Types;
using System;

namespace Cybersource.GraphQL
{
    [GraphQLMetadata("Mutation")]
    public class Mutation : ObjectGraphType<object>
    {
        public Mutation(IVtexApiService vtexApiService)
        {
            Name = "Mutation";

            Field<StringGraphType>(
                "initConfiguration",
                resolve: context =>
                {
                    return vtexApiService.InitConfiguration();
                });

            Field<StringGraphType>(
                "removeConfiguration",
                resolve: context =>
                {
                    return vtexApiService.RemoveConfiguration();
                });
        }
    }
}
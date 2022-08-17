using Cybersource.Data;
using Cybersource.GraphQL.Types;
using Cybersource.Models;
using Cybersource.Services;
using GraphQL;
using GraphQL.Types;
using System;
using System.Net;

namespace Cybersource.GraphQL
{
    [GraphQLMetadata("Mutation")]
    public class Mutation : ObjectGraphType<object>
    {
        public Mutation(IVtexApiService vtexApiService, ICybersourceRepository cybersourceRepository)
        {
            Name = "Mutation";

            FieldAsync<StringGraphType>(
                "initConfiguration",
                resolve: async context =>
                {
                    HttpStatusCode isValidAuthUser = await vtexApiService.IsValidAuthUser();

                    if (isValidAuthUser != HttpStatusCode.OK)
                    {
                        context.Errors.Add(new ExecutionError(isValidAuthUser.ToString())
                        {
                            Code = isValidAuthUser.ToString()
                        });

                        return default;
                    }

                    return await vtexApiService.InitConfiguration();
                });

            FieldAsync<StringGraphType>(
                "removeConfiguration",
                resolve: async context =>
                {
                    HttpStatusCode isValidAuthUser = await vtexApiService.IsValidAuthUser();

                    if (isValidAuthUser != HttpStatusCode.OK)
                    {
                        context.Errors.Add(new ExecutionError(isValidAuthUser.ToString())
                        {
                            Code = isValidAuthUser.ToString()
                        });

                        return default;
                    }

                    return await vtexApiService.RemoveConfiguration();
                });
        }
    }
}
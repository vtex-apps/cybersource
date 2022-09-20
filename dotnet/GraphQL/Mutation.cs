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
        public Mutation(IVtexApiService vtexApiService, ICybersourceRepository cybersourceRepository, ICybersourcePaymentService cybersourcePaymentService)
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

            FieldAsync<StringGraphType>(
                "payerAuthorize",
                arguments: new QueryArguments(
                    new QueryArgument<StringGraphType> { Name = "paymentId" }
                    ),
                resolve: async context =>
                {
                    string paymentId = context.GetArgument<string>("paymentId");
                    Console.WriteLine($"    payerAuthorize paymentId = '{paymentId}'    ");
                    CreatePaymentResponse paymentResponse = new CreatePaymentResponse();
                    PaymentData paymentData = await cybersourceRepository.GetPaymentData(paymentId);
                    if (paymentData != null && paymentData.CreatePaymentRequest != null)
                    {
                        if (!string.IsNullOrEmpty(paymentData.PayerAuthReferenceId))
                        {
                            paymentResponse = await cybersourcePaymentService.CreatePayment(paymentData.CreatePaymentRequest);
                        }
                        else
                        {
                            paymentResponse = new CreatePaymentResponse
                            {
                                Message = "Missing PayerAuthReferenceId"
                            };
                        }
                    }

                    Console.WriteLine($"    payerAuthorize paymentResponse.Status = '{paymentResponse.Status}'    ");
                    return paymentResponse.Status; // approved, denied, undefined
                });
        }
    }
}
using Cybersource.Data;
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
                    CreatePaymentResponse createPaymentResponse = new CreatePaymentResponse();
                    PaymentsResponse paymentsResponse = null;
                    PaymentData paymentData = await cybersourceRepository.GetPaymentData(paymentId);
                    if (paymentData != null && paymentData.CreatePaymentRequest != null)
                    {
                        if (!string.IsNullOrEmpty(paymentData.PayerAuthReferenceId))
                        {
                            (createPaymentResponse, paymentsResponse) = await cybersourcePaymentService.CreatePayment(paymentData.CreatePaymentRequest);
                        }
                        else
                        {
                            createPaymentResponse = new CreatePaymentResponse
                            {
                                Message = "Missing PayerAuthReferenceId"
                            };
                        }
                    }

                    Console.WriteLine($"    payerAuthorize paymentResponse.Status = '{createPaymentResponse.Status}'    ");
                    return createPaymentResponse.Status; // approved, denied, undefined
                });
        }
    }
}
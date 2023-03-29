using GraphQL;
using GraphQL.Types;
using Cybersource.Models;

namespace Cybersource.GraphQL.Types
{
    [GraphQLMetadata("AppSettings")]
    public class AppSettingsType : ObjectGraphType<MerchantSettings>
    {
        public AppSettingsType()
        {
            Name = "AppSettings";

            Field(b => b.IsLive).Description("Production");
            Field(b => b.MerchantId, nullable: true).Description("Cybersource MID");
            Field(b => b.MerchantKey, nullable: true).Description("Cybersource Key");
            Field(b => b.SharedSecretKey, nullable: true).Description("Cybersource Secret");
            Field(b => b.Processor, nullable: true).Description("Processor");
            Field(b => b.Region, nullable: true).Description("Region");
            Field(b => b.OrderSuffix, nullable: true).Description("Optional Order Reference Suffix");
            Field(b => b.CustomNsu, nullable: true).Description("Override default NSU with custom value");
            Field(b => b.EnableTax).Description("Enable Tax");
            Field(b => b.EnableTransactionPosting, nullable: true).Description("Enable transaction posting");
            Field(b => b.SalesChannelExclude, nullable: true).Description("Sales Channels to Exclude from Cybersource");
            Field(b => b.ShippingProductCode, nullable: true).Description("Shipping Product Code");
            Field(b => b.NexusRegions, nullable: true).Description("Tax Nexus Regions");
        }
    }
}
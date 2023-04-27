using Cybersource.Models;
using GraphQL;
using GraphQL.Types;

namespace Cybersource.GraphQL.Types
{
    [GraphQLMetadata("MerchantDefinedValueSettingType")]
    public class MerchantDefinedValueSettingType : ObjectGraphType<MerchantDefinedValueSetting>
    {
        public MerchantDefinedValueSettingType()
        {
            Name = "MerchantDefinedValueSetting";
            Field(d => d.GoodPortion);
            Field(d => d.IsValid);
            Field(d => d.UserInput);
        }
    }
}

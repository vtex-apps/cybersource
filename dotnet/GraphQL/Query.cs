using Cybersource.Services;
using GraphQL;
using GraphQL.Types;

namespace Cybersource.GraphQL
{
    [GraphQLMetadata("Query")]
    public class Query : ObjectGraphType<object>
    {
        public Query(IVtexApiService vtexApiService)
        {
            Name = "Query";
        }
    }
}

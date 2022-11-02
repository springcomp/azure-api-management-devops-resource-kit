
namespace Microsoft.Azure.Management.ApiManagement.ArmTemplates.Common
{
    public class GraphQLSchemaTemplateResource : TemplateResource
    {
        public GraphQLSchemaTemplateResource()
        {
            properties = new GraphQLSchemaProperties();
        }

        public GraphQLSchemaProperties properties { get; set; }
    }

    public class GraphQLSchemaProperties
    {
        public GraphQLSchemaProperties()
        {
            document = new GraphQLSchemaDocument();
        }

        public string contentType { get; set; } = "application/vnd.ms-azure-apim.graphql.schema";
        public GraphQLSchemaDocument document { get; set; }
    }

    public class GraphQLSchemaDocument
    {
        public string value { get; set; }
    }
}
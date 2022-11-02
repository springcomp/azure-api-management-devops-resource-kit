using Microsoft.Azure.Management.ApiManagement.ArmTemplates.Common;
using System;

namespace Microsoft.Azure.Management.ApiManagement.ArmTemplates.Create
{
    public class GraphQLSchemaTemplateCreator : TemplateCreator
    {
        private readonly FileReader fileReader;
        public GraphQLSchemaTemplateCreator(FileReader fileReader)
        {
            this.fileReader = fileReader;
        }

        public GraphQLSchemaTemplateResource CreateGraphQLSchemaTemplateResource(APIConfig api, string[] dependsOn)
        {
            System.Diagnostics.Debug.Assert(!String.IsNullOrWhiteSpace(api.graphQLSchema));

            var schemaTemplateResource = new GraphQLSchemaTemplateResource()
            {
                name = $"[concat(parameters('{ParameterNames.ApimServiceName}'), '/{APITemplateCreator.MakeApiResourceName(api)}', '/graphql')]",
                type = ResourceTypeConstants.APISchema,
                apiVersion = GlobalConstants.APIVersion,
                properties = new GraphQLSchemaProperties
                {
                    document = new GraphQLSchemaDocument
                    {
                        value = this.fileReader.RetrieveLocalFileContents(api.graphQLSchema),
                    }
                },
                dependsOn = dependsOn
            };
            return schemaTemplateResource;
        }
    }
}

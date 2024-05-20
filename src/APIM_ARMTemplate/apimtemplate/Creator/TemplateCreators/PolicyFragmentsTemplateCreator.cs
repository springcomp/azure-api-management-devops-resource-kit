using System.Collections.Generic;
using Microsoft.Azure.Management.ApiManagement.ArmTemplates.Common;

namespace Microsoft.Azure.Management.ApiManagement.ArmTemplates.Create
{
    public class PolicyFragmentsTemplateCreator : TemplateCreator
    {
        private FileReader fileReader;

        public PolicyFragmentsTemplateCreator(FileReader fileReader)
        {
            this.fileReader = fileReader;
        }

        public Template CreatePolicyFragmentsTemplate(CreatorConfig creatorConfig)
        {
            // create empty template
            Template policyTemplate = CreateEmptyTemplate();

            // add parameters
            policyTemplate.parameters = new Dictionary<string, TemplateParameterProperties>
            {
                { ParameterNames.ApimServiceName, new TemplateParameterProperties(){ type = "string" } }
            };

            // create policy fragment resources with properties

            policyTemplate.resources = CreatePolicyFragmentsTemplateResources(creatorConfig.fragments);

            return policyTemplate;
        }

        public TemplateResource[] CreatePolicyFragmentsTemplateResources(List<PolicyFragment> fragments)
        {
            List<TemplateResource> resources = new List<TemplateResource>();

            foreach (var fragment in fragments)
            {
                resources.Add(new PolicyFragmentTemplateResource
                {
                    name = $"[concat(parameters('{ParameterNames.ApimServiceName}'), '/{fragment.name}')]",
                    type = ResourceTypeConstants.PolicyFragments,
                    apiVersion = GlobalConstants.APIVersion,
                    properties = new PolicyFragmentProperties
                    {
                        description = fragment.description,
                        value = this.fileReader.RetrieveLocalFileContents(fragment.policy)
                    },
                    dependsOn = [],
                });
            }

            return [.. resources];
        }
    }
}

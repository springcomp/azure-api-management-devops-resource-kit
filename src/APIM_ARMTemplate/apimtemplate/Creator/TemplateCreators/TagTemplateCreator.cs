using System.Collections.Generic;
using Microsoft.Azure.Management.ApiManagement.ArmTemplates.Common;
using System;
using System.Linq;

namespace Microsoft.Azure.Management.ApiManagement.ArmTemplates.Create
{
    public class TagTemplateCreator : TemplateCreator
    {
        public Template CreateTagTemplate(CreatorConfig creatorConfig)
        {
            // create empty template
            Template tagTemplate = CreateEmptyTemplate();

            // add parameters
            tagTemplate.parameters = new Dictionary<string, TemplateParameterProperties>
            {
                [ParameterNames.ApimServiceName] = new TemplateParameterProperties(){ type = "string" },
            };

            // aggregate all tags from apis
            HashSet<string> tagHashset = [];
            List<APIConfig> apis = creatorConfig.apis;
            if (apis != null)
            {
                foreach (APIConfig api in apis)
                {
                    if (api.tags != null)
                    {
                        string[] apiTags = api.tags.Split(", ");
                        foreach (string apiTag in apiTags)
                            tagHashset.Add(apiTag);
                    }
                }
            }

            foreach (TagTemplateProperties tag in creatorConfig.tags)
                tagHashset.Add(tag.displayName);

            List<TemplateResource> resources = [];
            foreach (string tag in tagHashset)
            {
                // create tag resource with properties
                TagTemplateResource tagTemplateResource = new TagTemplateResource()
                {
                    name = $"[concat(parameters('{ParameterNames.ApimServiceName}'), '/{MakeTagName(tag)}')]",
                    type = ResourceTypeConstants.Tag,
                    apiVersion = GlobalConstants.APIVersion,
                    properties = new TagTemplateProperties() { displayName = tag, },
                    dependsOn = [],
                };
                resources.Add(tagTemplateResource);
            }

            tagTemplate.resources = [.. resources];

            return tagTemplate;
        }
    }
}
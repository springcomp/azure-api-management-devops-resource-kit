using System.Collections.Generic;
using Microsoft.Azure.Management.ApiManagement.ArmTemplates.Common;

namespace Microsoft.Azure.Management.ApiManagement.ArmTemplates.Create
{
    public class TagResourceTemplateCreator : TemplateCreator
    {
        public List<TagTemplateResource> CreateTagProductTemplateResources(ProductConfig product, string[] dependsOn)
        { 
            string resourceName = product.name;
            string[] tags = product.tags.Split(", ");
            string resourceType = ResourceTypeConstants.ProductTag;
            return CreateTagResourceTemplateResources(resourceType, resourceName, tags, dependsOn);
        }
        public List<TagTemplateResource> CreateTagAPITemplateResources(APIConfig api, string[] dependsOn)
        { 
            string resourceName = APITemplateCreator.MakeApiResourceName(api);
            string[] tags = api.tags.Split(", ");
            string resourceType = ResourceTypeConstants.APITag;
            return CreateTagResourceTemplateResources(resourceType, resourceName, tags, dependsOn);
        }

        public List<TagTemplateResource> CreateTagResourceTemplateResources(string resourceType, string resourceName, string[] tagIDs, string[] dependsOn)
        {
            // create a tag/apis association resource for each tag in the config file
            List<TagTemplateResource> tagAPITemplates = [];
            // tags is comma seperated list of tags
            foreach(string tagID in tagIDs) 
            {
                TagTemplateResource tagAPITemplate = this.CreateTagResourceTemplateResource(tagID, resourceType, resourceName, dependsOn);
                tagAPITemplates.Add(tagAPITemplate);
            }
            return tagAPITemplates;
        }

        public TagTemplateResource CreateTagResourceTemplateResource(string tagName, string resourceType, string resourceName, string[] dependsOn)
            // create tags/apis resource with properties
            => new(){
                name = $"[concat(parameters('{ParameterNames.ApimServiceName}'), '/{resourceName}/{MakeTagName(tagName)}')]",
                type = resourceType,
                apiVersion = GlobalConstants.APIVersion,
                properties = new TagTemplateProperties(){ displayName = tagName, },
                dependsOn = dependsOn,
            };
    }
}
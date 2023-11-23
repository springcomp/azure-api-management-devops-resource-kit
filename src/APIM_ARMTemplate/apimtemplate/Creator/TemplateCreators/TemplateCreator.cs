using Microsoft.Azure.Management.ApiManagement.ArmTemplates.Common;
using System.Linq;

namespace Microsoft.Azure.Management.ApiManagement.ArmTemplates.Create
{
    public class TemplateCreator
    {
        public Template CreateEmptyTemplate()
        {
            // creates empty template for use in all other template creators
            Template template = new Template()
            {
                schema = "https://schema.management.azure.com/schemas/2019-04-01/deploymentTemplate.json#",
                contentVersion = "1.0.0.0",
                parameters = { },
                variables = { },
                resources = new TemplateResource[] { },
                outputs = { }
            };
            return template;
        }

        public Template CreateEmptyParameters()
        {
            // creates empty parameters file for use in all other template creators
            Template template = new Template()
            {
                schema = "https://schema.management.azure.com/schemas/2019-04-01/deploymentParameters.json#",
                contentVersion = "1.0.0.0",
                parameters = { },
            };
            return template;
        }

        protected string MakeTagName(string tag)
        {
            var invalids = tag.ToCharArray().Where(IsValidTagIdentifier);
            foreach (var c in invalids)
                tag = tag.Replace(c.ToString(), "-");
            return tag;
        }

            // https://learn.microsoft.com/en-us/azure/azure-resource-manager/management/resource-name-rules 
        private bool IsValidTagIdentifier(char c)
            => !"-1234567890ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz".ToCharArray().Contains(c);
    }
}

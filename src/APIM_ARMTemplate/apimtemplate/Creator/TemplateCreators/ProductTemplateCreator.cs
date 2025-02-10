using System;
using System.Collections.Generic;
using Microsoft.Azure.Management.ApiManagement.ArmTemplates.Common;

namespace Microsoft.Azure.Management.ApiManagement.ArmTemplates.Create
{
    public class ProductTemplateCreator : TemplateCreator
    {
        private PolicyTemplateCreator policyTemplateCreator;
        private ProductGroupTemplateCreator productGroupTemplateCreator;
        private SubscriptionTemplateCreator subscriptionTemplateCreator;
        private TagResourceTemplateCreator productTagTemplateCreator;

        public ProductTemplateCreator(
            PolicyTemplateCreator policyTemplateCreator,
            ProductGroupTemplateCreator productGroupTemplateCreator,
            SubscriptionTemplateCreator subscriptionTemplateCreator,
            TagResourceTemplateCreator productTagTemplateCreator
            )
        {
            this.policyTemplateCreator = policyTemplateCreator;
            this.productGroupTemplateCreator = productGroupTemplateCreator;
            this.subscriptionTemplateCreator = subscriptionTemplateCreator;
            this.productTagTemplateCreator = productTagTemplateCreator;
        }

        public Template CreateProductTemplate(CreatorConfig creatorConfig)
        {
            // create empty template
            Template productTemplate = CreateEmptyTemplate();

            // add parameters
            productTemplate.parameters = new Dictionary<string, TemplateParameterProperties>
            {
                { ParameterNames.ApimServiceName, new TemplateParameterProperties(){ type = "string" } }
            };

            List<TemplateResource> resources = new List<TemplateResource>();
            foreach (ProductConfig product in creatorConfig.products)
            {
                if (string.IsNullOrEmpty(product.name)) {
                    product.name = product.displayName;
                }
                // create product resource with properties
                ProductsTemplateResource productsTemplateResource = new ProductsTemplateResource()
                {
                    name = $"[concat(parameters('{ParameterNames.ApimServiceName}'), '/{product.name}')]",
                    type = ResourceTypeConstants.Product,
                    apiVersion = GlobalConstants.APIVersion,
                    properties = new ProductsTemplateProperties()
                    {
                        description = product.description,
                        terms = product.terms,
                        subscriptionRequired = product.subscriptionRequired,
                        approvalRequired = product.subscriptionRequired ? product.approvalRequired : null,
                        subscriptionsLimit = product.subscriptionRequired ? product.subscriptionsLimit : null,
                        state = product.state,
                        displayName = product.displayName
                    },
                    dependsOn = new string[] { }
                };
                resources.Add(productsTemplateResource);

                string[] dependsOn = [$"[resourceId('Microsoft.ApiManagement/service/products', parameters('{ParameterNames.ApimServiceName}'), '{product.name}')]"];

                // create product policy resource that depends on the product, if provided
                if (product.policy != null)
                    resources.Add(this.policyTemplateCreator.CreateProductPolicyTemplateResource(product, dependsOn));

                // create product group resources if provided
                if (product.groups != null)
                    resources.AddRange(this.productGroupTemplateCreator.CreateProductGroupTemplateResources(product, dependsOn));

                // create product subscriptions if provided
                if (product.subscriptions != null)
                    resources.AddRange(this.subscriptionTemplateCreator.CreateSubscriptionsTemplateResources(product, dependsOn));

                // create product tags if provided
                if (!string.IsNullOrWhiteSpace(product.tags))
                    resources.AddRange(this.productTagTemplateCreator.CreateTagProductTemplateResources(product, dependsOn));
            }

            productTemplate.resources = resources.ToArray();
            return productTemplate;
        }
    }
}
using System;
using System.Collections.Generic;
using Microsoft.Azure.Management.ApiManagement.ArmTemplates.Common;

namespace Microsoft.Azure.Management.ApiManagement.ArmTemplates.Create
{
    public class SubscriptionTemplateCreator : TemplateCreator
    {
        public SubscriptionsTemplateResource CreateSubscriptionsTemplateResource(SubscriptionConfig subscription, string[] dependsOn)
        {
            return new SubscriptionsTemplateResource
            {
                name = $"[concat(parameters('ApimServiceName'), '/{subscription.name}')]",
                type = "Microsoft.ApiManagement/service/subscriptions",
                apiVersion = GlobalConstants.APIVersion,
                properties = new SubscriptionsTemplateProperties
                {
                    ownerId = subscription.ownerId,
                    scope = subscription.scope,
                    displayName = subscription.displayName,
                    primaryKey = subscription.primaryKey,
                    secondaryKey = subscription.secondaryKey,
                    state = subscription.state,
                    allowTracing = subscription.allowTracing,
                },
                dependsOn = dependsOn,
            };
        }

        public List<SubscriptionsTemplateResource> CreateSubscriptionsTemplateResources(ProductConfig product, string[] dependsOn)
        { 
            if(dependsOn?.Length != 1)            
                throw new ApplicationException("A subscription can only depend on one single product");

            var scope = dependsOn[0];

            var resources = new List<SubscriptionsTemplateResource>(product.subscriptions.Count);

            foreach (var subscription in product.subscriptions)
            {
                subscription.scope = scope;
                resources.Add(CreateSubscriptionsTemplateResource(subscription, dependsOn));
            }

            return resources;
        }
    }
}
﻿using System.Collections.Generic;
using Microsoft.Azure.Management.ApiManagement.ArmTemplates.Common;
using System.Threading.Tasks;
using System;
using System.Linq;

namespace Microsoft.Azure.Management.ApiManagement.ArmTemplates.Create
{
    public class MasterTemplateCreator : TemplateCreator
    {
        private readonly string deploySuffix_;

        public MasterTemplateCreator()
        {

        }
        public MasterTemplateCreator(string deploySuffix)
        {
            if (!String.IsNullOrEmpty(deploySuffix))
                this.deploySuffix_ = $"-{deploySuffix}";
        }


        public Template CreateLinkedMasterTemplate(CreatorConfig creatorConfig,
            Template globalServicePolicyTemplate,
            Template apiVersionSetTemplate,
            Template productsTemplate,
            Template productAPIsTemplate,
            Template propertyTemplate,
            Template fragmentsTemplate,
            Template loggersTemplate,
            Template backendsTemplate,
            Template authorizationServersTemplate,
            Template tagTemplate,
            List<LinkedMasterTemplateAPIInformation> apiInformation,
            FileNames fileNames,
            string apimServiceName,
            FileNameGenerator fileNameGenerator)
        {
            // create empty template
            Template masterTemplate = CreateEmptyTemplate();

            // add parameters
            masterTemplate.parameters = this.CreateMasterTemplateParameters(creatorConfig);

            // add deployment resources that links to all resource files
            List<TemplateResource> resources = new List<TemplateResource>();

            // globalServicePolicy
            if (globalServicePolicyTemplate != null)
            {
                string globalServicePolicyUri = GenerateLinkedTemplateUri(creatorConfig, fileNames.globalServicePolicy);
                resources.Add(this.CreateLinkedMasterTemplateResource("globalServicePolicyTemplate", globalServicePolicyUri, new string[] { }, null, false));
            }

            // apiVersionSet
            if (apiVersionSetTemplate != null)
            {
                string apiVersionSetUri = GenerateLinkedTemplateUri(creatorConfig, fileNames.apiVersionSets);
                resources.Add(this.CreateLinkedMasterTemplateResource("versionSetTemplate", apiVersionSetUri, new string[] { }, null, false));
            }

            // product
            if (productsTemplate != null)
            {
                string[] dependsOn = [];
                if (propertyTemplate?.resources?.Length > 0)
                    dependsOn = [.. dependsOn, $"[resourceId('Microsoft.Resources/deployments', 'propertyTemplate{deploySuffix_}')]",];
                if (fragmentsTemplate?.resources?.Length > 0)
                    dependsOn = [.. dependsOn, $"[resourceId('Microsoft.Resources/deployments', 'fragmentsTemplate{deploySuffix_}')]",];

                string productsUri = GenerateLinkedTemplateUri(creatorConfig, fileNames.products);
                resources.Add(this.CreateLinkedMasterTemplateResource("productsTemplate", productsUri, dependsOn, null, false));
            }

            // productApi
            if (productAPIsTemplate != null)
            {
                // depends on all products and APIs
                string[] dependsOn = CreateProductAPIResourceDependencies(productsTemplate, apiInformation, fileNameGenerator);
                string productAPIsUri = GenerateLinkedTemplateUri(creatorConfig, fileNames.productAPIs);
                resources.Add(this.CreateLinkedMasterTemplateResource("productAPIsTemplate", productAPIsUri, dependsOn, null, false));
            }

            // property
            if (propertyTemplate != null)
            {
                string propertyUri = GenerateLinkedTemplateUri(creatorConfig, fileNames.namedValues);
                resources.Add(this.CreateLinkedMasterTemplateResource("propertyTemplate", propertyUri, new string[] { }, null, false));
            }

            // policyFragments
            if (fragmentsTemplate != null)
            {
                string[] dependsOn = [];
                if (propertyTemplate?.resources?.Length > 0)
                    dependsOn = [.. dependsOn, $"[resourceId('Microsoft.Resources/deployments', 'propertyTemplate{deploySuffix_}')]",];

                string fragmentsUri = GenerateLinkedTemplateUri(creatorConfig, fileNames.fragments);
                resources.Add(this.CreateLinkedMasterTemplateResource("fragmentsTemplate", fragmentsUri, dependsOn, null, false));
            }

            // logger
            if (loggersTemplate != null)
            {
                string loggersUri = GenerateLinkedTemplateUri(creatorConfig, fileNames.loggers);
                resources.Add(this.CreateLinkedMasterTemplateResource("loggersTemplate", loggersUri, new string[] { }, null, false));
            }

            // backend
            if (backendsTemplate != null)
            {
                string backendsUri = GenerateLinkedTemplateUri(creatorConfig, fileNames.backends);
                resources.Add(this.CreateLinkedMasterTemplateResource("backendsTemplate", backendsUri, new string[] { }, null, false));
            }

            // authorizationServer
            if (authorizationServersTemplate != null)
            {
                string authorizationServersUri = GenerateLinkedTemplateUri(creatorConfig, fileNames.authorizationServers);
                resources.Add(this.CreateLinkedMasterTemplateResource("authorizationServersTemplate", authorizationServersUri, new string[] { }, null, false));
            }

            // tag
            if (tagTemplate != null)
            {
                string tagUri = GenerateLinkedTemplateUri(creatorConfig, fileNames.tags);
                resources.Add(this.CreateLinkedMasterTemplateResource("tagTemplate", tagUri, new string[] { }, null, false));
            }

            string previousAPIName = null;
            // each api has an associated api info class that determines whether the api is split and its dependencies on other resources
            foreach (LinkedMasterTemplateAPIInformation apiInfo in apiInformation)
            {
                if (apiInfo.isSplit == true)
                {
                    // add a deployment resource for both api template files
                    string originalAPIName = fileNameGenerator.GenerateOriginalAPIName(apiInfo.name);
                    string subsequentAPIDeploymentResourceName = $"{originalAPIName}-SubsequentAPITemplate";
                    string initialAPIDeploymentResourceName = $"{originalAPIName}-InitialAPITemplate";

                    if (apiInfo.hasInitialRevisionOrVersion)
                    {
                        string initialAPIFileName = fileNameGenerator.GenerateCreatorAPIFileName(apiInfo.name, apiInfo.isSplit, true);
                        string initialAPIUri = GenerateLinkedTemplateUri(creatorConfig, initialAPIFileName);
                        string[] initialAPIDependsOn = CreateAPIResourceDependencies(
                            creatorConfig,
                            globalServicePolicyTemplate,
                            apiVersionSetTemplate,
                            productsTemplate,
                            propertyTemplate,
                            fragmentsTemplate,
                            loggersTemplate,
                            backendsTemplate,
                            authorizationServersTemplate,
                            tagTemplate,
                            apiInfo,
                            previousAPIName,
                            apiInformation);

                        resources.Add(this.CreateLinkedMasterTemplateResource(
                            initialAPIDeploymentResourceName,
                            initialAPIUri,
                            initialAPIDependsOn,
                            originalAPIName,
                            apiInfo.isServiceUrlParameterize));
                    }

                    if (apiInfo.hasRevision)
                    {
                        string subsequentAPIFileName = fileNameGenerator.GenerateCreatorAPIFileName(apiInfo.name, apiInfo.isSplit, false);
                        string subsequentAPIUri = GenerateLinkedTemplateUri(creatorConfig, subsequentAPIFileName);
                        string[] subsequentAPIDependsOn = [];
                        if (apiInfo.hasInitialRevisionOrVersion)
                            subsequentAPIDependsOn = [ $"[resourceId('Microsoft.Resources/deployments', '{initialAPIDeploymentResourceName}{deploySuffix_}')]" ];

                        resources.Add(this.CreateLinkedMasterTemplateResource(
                            subsequentAPIDeploymentResourceName,
                            subsequentAPIUri,
                            subsequentAPIDependsOn,
                            originalAPIName,
                            apiInfo.isServiceUrlParameterize));
                    }

                }
                else
                {
                    // add a deployment resource for the unified api template file
                    string originalAPIName = fileNameGenerator.GenerateOriginalAPIName(apiInfo.name);
                    string subsequentAPIDeploymentResourceName = $"{originalAPIName}-SubsequentAPITemplate{deploySuffix_}";
                    string unifiedAPIDeploymentResourceName = $"{originalAPIName}-APITemplate";
                    string unifiedAPIFileName = fileNameGenerator.GenerateCreatorAPIFileName(apiInfo.name, apiInfo.isSplit, true);
                    string unifiedAPIUri = GenerateLinkedTemplateUri(creatorConfig, unifiedAPIFileName);
                    string[] unifiedAPIDependsOn = CreateAPIResourceDependencies(
                        creatorConfig,
                        globalServicePolicyTemplate,
                        apiVersionSetTemplate,
                        productsTemplate,
                        propertyTemplate,
                        fragmentsTemplate,
                        loggersTemplate,
                        backendsTemplate,
                        authorizationServersTemplate,
                        tagTemplate,
                        apiInfo,
                        previousAPIName,
                        apiInformation);

                    resources.Add(this.CreateLinkedMasterTemplateResource(
                        unifiedAPIDeploymentResourceName,
                        unifiedAPIUri,
                        unifiedAPIDependsOn,
                        originalAPIName,
                        apiInfo.isServiceUrlParameterize));

                    // Set previous API name for dependency chain
                    previousAPIName = subsequentAPIDeploymentResourceName;
                }
            }

            masterTemplate.resources = resources.ToArray();
            return masterTemplate;
        }

        public string[] CreateAPIResourceDependencies(
            CreatorConfig creatorConfig,
            Template globalServicePolicyTemplate,
            Template apiVersionSetTemplate,
            Template productsTemplate,
            Template propertyTemplate,
            Template policyFragmentsTemplate,
            Template loggersTemplate,
            Template backendsTemplate,
            Template authorizationServersTemplate,
            Template tagTemplate,
            LinkedMasterTemplateAPIInformation apiInfo,
            string previousAPI,
            List<LinkedMasterTemplateAPIInformation> apiInformation)
        {
            List<string> apiDependsOn = new List<string>();
            if (globalServicePolicyTemplate != null && apiInfo.dependsOnGlobalServicePolicies == true)
            {
                apiDependsOn.Add($"[resourceId('Microsoft.Resources/deployments', 'globalServicePolicyTemplate{deploySuffix_}')]");
            }
            if (apiVersionSetTemplate != null && apiInfo.dependsOnVersionSets == true)
            {
                apiDependsOn.Add($"[resourceId('Microsoft.Resources/deployments', 'versionSetTemplate{deploySuffix_}')]");
            }
            if (productsTemplate != null && apiInfo.dependsOnProducts == true)
            {
                apiDependsOn.Add($"[resourceId('Microsoft.Resources/deployments', 'productsTemplate{deploySuffix_}')]");
            }
            if (propertyTemplate?.resources?.Length > 0)
            {
                apiDependsOn.Add($"[resourceId('Microsoft.Resources/deployments', 'propertyTemplate{deploySuffix_}')]");
            }
            if (policyFragmentsTemplate?.resources?.Length > 0)
            {
                apiDependsOn.Add($"[resourceId('Microsoft.Resources/deployments', 'fragmentsTemplate{deploySuffix_}')]");
            }
            if (loggersTemplate != null && apiInfo.dependsOnLoggers == true)
            {
                apiDependsOn.Add($"[resourceId('Microsoft.Resources/deployments', 'loggersTemplate{deploySuffix_}')]");
            }
            if (backendsTemplate != null && apiInfo.dependsOnBackends == true)
            {
                apiDependsOn.Add($"[resourceId('Microsoft.Resources/deployments', 'backendsTemplate{deploySuffix_}')]");
            }
            if (authorizationServersTemplate != null && apiInfo.dependsOnAuthorizationServers == true)
            {
                apiDependsOn.Add($"[resourceId('Microsoft.Resources/deployments', 'authorizationServersTemplate{deploySuffix_}')]");
            }
            if (tagTemplate != null && apiInfo.dependsOnTags == true)
            {
                apiDependsOn.Add($"[resourceId('Microsoft.Resources/deployments', 'tagTemplate{deploySuffix_}')]");
            }
            if (apiInfo.dependsOnVersion != null)
            {
                var dependentVersion = apiInformation.First(a => a.name == apiInfo.dependsOnVersion);
                if (dependentVersion.hasInitialRevisionOrVersion)
                    apiDependsOn.Add($"[resourceId('Microsoft.Resources/deployments', '{apiInfo.dependsOnVersion}-InitialAPITemplate{deploySuffix_}')]");
                else if (dependentVersion.hasRevision)
                    apiDependsOn.Add($"[resourceId('Microsoft.Resources/deployments', '{apiInfo.dependsOnVersion}-SubsequentAPITemplate{deploySuffix_}')]");
            }
            if (previousAPI != null && apiInfo.dependsOnTags == true)
            {
                apiDependsOn.Add($"[resourceId('Microsoft.Resources/deployments', '{previousAPI}')]");
            }
            return apiDependsOn.ToArray();
        }

        public string[] CreateProductAPIResourceDependencies(Template productsTemplate,
            List<LinkedMasterTemplateAPIInformation> apiInformation,
            FileNameGenerator fileNameGenerator)
        {
            List<string> apiProductDependsOn = new List<string>();
            if (productsTemplate != null)
            {
                apiProductDependsOn.Add($"[resourceId('Microsoft.Resources/deployments', 'productsTemplate{deploySuffix_}')]");
            }
            foreach (LinkedMasterTemplateAPIInformation apiInfo in apiInformation)
            {
                string originalAPIName = fileNameGenerator.GenerateOriginalAPIName(apiInfo.name);
                string apiDeploymentResourceName = apiInfo.isSplit ?
                                                                    apiInfo.hasRevision
                                                                        ? $"{originalAPIName}-SubsequentAPITemplate{deploySuffix_}"
                                                                        : $"{originalAPIName}-InitialAPITemplate{deploySuffix_}"
                                                                   : $"{originalAPIName}-APITemplate{deploySuffix_}";
                apiProductDependsOn.Add($"[resourceId('Microsoft.Resources/deployments', '{apiDeploymentResourceName}')]");
            }
            return apiProductDependsOn.ToArray();
        }

        public MasterTemplateResource CreateLinkedMasterTemplateResource(string name, string uriLink, string[] dependsOn, string apiName, bool isServiceUrlParameterizeInApi)
        {
            // create deployment resource with provided arguments
            MasterTemplateResource masterTemplateResource = new MasterTemplateResource()
            {
                name = $"{name}{deploySuffix_}",
                type = "Microsoft.Resources/deployments",
                apiVersion = GlobalConstants.LinkedAPIVersion,
                properties = new MasterTemplateProperties()
                {
                    mode = "Incremental",
                    templateLink = new MasterTemplateLink()
                    {
                        uri = uriLink,
                        contentVersion = "1.0.0.0"
                    },
                    parameters = new Dictionary<string, TemplateParameterProperties>
                    {
                        { ParameterNames.ApimServiceName, new TemplateParameterProperties(){ value = $"[parameters('{ParameterNames.ApimServiceName}')]" } }
                    }
                },
                dependsOn = dependsOn
            };

            if (name.IndexOf("APITemplate") > 0 && isServiceUrlParameterizeInApi)
            {
                TemplateParameterProperties serviceUrlParamProperty = new TemplateParameterProperties()
                {
                    value = $"[parameters('{apiName}-ServiceUrl')]"
                };
                masterTemplateResource.properties.parameters.Add(apiName + "-ServiceUrl", serviceUrlParamProperty);
            }

            return masterTemplateResource;
        }

        public string GetDependsOnPreviousApiVersion(APIConfig api, IDictionary<string, string[]> apiVersions)
        {
            if (api?.apiVersionSetId == null)
                return null;

            // get all apis associated with the same versionSet
            // versions must be deployed in sequence and thus
            // each api must depend on the previous version.

            var versions = apiVersions.ContainsKey(api.apiVersionSetId)
                ? apiVersions[api.apiVersionSetId]
                : null
                ;

            var index = Array.IndexOf(versions, api.name);
            var previous = index > 0
                ? (int?)index - 1
                : null
                ;

            return previous.HasValue
                ? versions[previous.Value]
                : null
                ;
        }

        public Dictionary<string, TemplateParameterProperties> CreateMasterTemplateParameters(CreatorConfig creatorConfig)
        {
            // used to create the parameter metatadata, etc (not value) for use in file with resources
            // add parameters with metadata properties
            Dictionary<string, TemplateParameterProperties> parameters = new Dictionary<string, TemplateParameterProperties>();
            TemplateParameterProperties apimServiceNameProperties = new TemplateParameterProperties()
            {
                metadata = new TemplateParameterMetadata()
                {
                    description = "Name of the API Management"
                },
                type = "string"
            };
            parameters.Add(ParameterNames.ApimServiceName, apimServiceNameProperties);
            // add remote location of template files for linked option
            if (creatorConfig.linked == true)
            {
                TemplateParameterProperties linkedTemplatesBaseUrlProperties = new TemplateParameterProperties()
                {
                    metadata = new TemplateParameterMetadata()
                    {
                        description = "Base URL of the repository"
                    },
                    type = "string"
                };
                parameters.Add(ParameterNames.LinkedTemplatesBaseUrl, linkedTemplatesBaseUrlProperties);
                if (creatorConfig.linkedTemplatesUrlQueryString != null)
                {
                    TemplateParameterProperties linkedTemplatesUrlQueryStringProperties = new TemplateParameterProperties()
                    {
                        metadata = new TemplateParameterMetadata()
                        {
                            description = "Query string for the URL of the repository"
                        },
                        type = "string"
                    };
                    parameters.Add(ParameterNames.LinkedTemplatesUrlQueryString, linkedTemplatesUrlQueryStringProperties);
                }
            }

            // add serviceUrl parameter for linked option
            if (creatorConfig.serviceUrlParameters != null && creatorConfig.serviceUrlParameters.Count > 0)
            {
                foreach (ServiceUrlProperty serviceUrlProperty in creatorConfig.serviceUrlParameters)
                {
                    TemplateParameterProperties serviceUrlParamProperty = new TemplateParameterProperties()
                    {
                        metadata = new TemplateParameterMetadata()
                        {
                            description = "ServiceUrl parameter for API: " + serviceUrlProperty.apiName
                        },
                        type = "string"
                    };
                    parameters.Add(serviceUrlProperty.apiName + "-ServiceUrl", serviceUrlParamProperty);
                }
            }

            return parameters;
        }

        public Template CreateMasterTemplateParameterValues(CreatorConfig creatorConfig)
        {
            // used to create the parameter values for use in parameters file
            // create empty template
            Template masterTemplate = CreateEmptyParameters();

            // add parameters with value property
            Dictionary<string, TemplateParameterProperties> parameters = new Dictionary<string, TemplateParameterProperties>();
            TemplateParameterProperties apimServiceNameProperties = new TemplateParameterProperties()
            {
                value = creatorConfig.apimServiceName
            };
            parameters.Add(ParameterNames.ApimServiceName, apimServiceNameProperties);
            if (creatorConfig.linked == true)
            {
                TemplateParameterProperties linkedTemplatesBaseUrlProperties = new TemplateParameterProperties()
                {
                    value = creatorConfig.linkedTemplatesBaseUrl
                };
                parameters.Add(ParameterNames.LinkedTemplatesBaseUrl, linkedTemplatesBaseUrlProperties);
                if (creatorConfig.linkedTemplatesUrlQueryString != null)
                {
                    TemplateParameterProperties linkedTemplatesUrlQueryStringProperties = new TemplateParameterProperties()
                    {
                        value = creatorConfig.linkedTemplatesUrlQueryString
                    };
                    parameters.Add(ParameterNames.LinkedTemplatesUrlQueryString, linkedTemplatesUrlQueryStringProperties);
                }
            }

            if (creatorConfig.serviceUrlParameters != null && creatorConfig.serviceUrlParameters.Count > 0)
            {
                foreach (ServiceUrlProperty serviceUrlProperty in creatorConfig.serviceUrlParameters)
                {
                    TemplateParameterProperties serviceUrlParamProperty = new TemplateParameterProperties()
                    {
                        value = serviceUrlProperty.serviceUrl
                    };
                    parameters.Add(serviceUrlProperty.apiName + "-ServiceUrl", serviceUrlParamProperty);
                }
            }

            masterTemplate.parameters = parameters;
            return masterTemplate;
        }

        public async Task<bool> DetermineIfAPIDependsOnLoggerAsync(APIConfig api, FileReader fileReader)
        {
            if (api.diagnostic != null && api.diagnostic.loggerId != null)
            {
                // capture api diagnostic dependent on logger
                return true;
            }
            string apiPolicy = api.policy != null ? await fileReader.RetrieveFileContentsAsync(api.policy) : "";
            if (apiPolicy.Contains("logger"))
            {
                // capture api policy dependent on logger
                return true;
            }
            if (api.operations != null)
            {
                foreach (KeyValuePair<string, OperationsConfig> operation in api.operations)
                {
                    string operationPolicy = operation.Value.policy != null ? await fileReader.RetrieveFileContentsAsync(operation.Value.policy) : "";
                    if (operationPolicy.Contains("logger"))
                    {
                        // capture operation policy dependent on logger
                        return true;
                    }
                }
            }
            return false;
        }

        public async Task<bool> DetermineIfAPIDependsOnBackendAsync(APIConfig api, FileReader fileReader)
        {
            string apiPolicy = api.policy != null ? await fileReader.RetrieveFileContentsAsync(api.policy) : "";
            if (apiPolicy.Contains("set-backend-service"))
            {
                // capture api policy dependent on backend
                return true;
            }
            if (api.operations != null)
            {
                foreach (KeyValuePair<string, OperationsConfig> operation in api.operations)
                {
                    string operationPolicy = operation.Value.policy != null ? await fileReader.RetrieveFileContentsAsync(operation.Value.policy) : "";
                    if (operationPolicy.Contains("set-backend-service"))
                    {
                        // capture operation policy dependent on backend
                        return true;
                    }
                }
            }
            return false;
        }

        public string GenerateLinkedTemplateUri(CreatorConfig creatorConfig, string fileName)
        {
            return creatorConfig.linkedTemplatesUrlQueryString != null ?
             $"[concat(parameters('{ParameterNames.LinkedTemplatesBaseUrl}'), '{fileName}', parameters('{ParameterNames.LinkedTemplatesUrlQueryString}'))]"
             : $"[concat(parameters('{ParameterNames.LinkedTemplatesBaseUrl}'), '{fileName}')]";
        }
    }

    public class LinkedMasterTemplateAPIInformation
    {
        public string name { get; set; }
        public bool isSplit { get; set; }
        public bool dependsOnGlobalServicePolicies { get; set; }
        public bool dependsOnVersionSets { get; set; }
        public bool dependsOnProducts { get; set; }
        public bool dependsOnLoggers { get; set; }
        public bool dependsOnBackends { get; set; }
        public bool dependsOnAuthorizationServers { get; set; }
        public bool dependsOnTags { get; set; }
        public bool isServiceUrlParameterize { get; set; }
        public string dependsOnVersion { get; set; }

        public bool hasRevision { get; set; }
        public bool hasInitialRevisionOrVersion { get; set; }

    }

}

using McMaster.Extensions.CommandLineUtils;
using System;
using Microsoft.Azure.Management.ApiManagement.ArmTemplates.Common;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;
using apimtemplate.Creator.Utilities;
using System.IO;
using System.Text.RegularExpressions;

namespace Microsoft.Azure.Management.ApiManagement.ArmTemplates.Create
{
    public class CreateCommand : CommandLineApplication
    {
        public CreateCommand()
        {
            this.Name = GlobalConstants.CreateName;
            this.Description = GlobalConstants.CreateDescription;

            CommandOption appInsightsInstrumentationKey = this.Option("--appInsightsInstrumentationKey <appInsightsInstrumentationKey>", "AppInsights intrumentationkey", CommandOptionType.SingleValue);

            CommandOption appInsightsName = this.Option("--appInsightsName <appInsightsName>", "AppInsights Name", CommandOptionType.SingleValue);

            // Allow Named values to pass as parameters
            CommandOption namedValueKeys = this.Option("--namedValues <namedValues>", "Named Values", CommandOptionType.SingleValue);

            // apimNameValue values to pass as parameters
            CommandOption apimNameValue = this.Option("--apimNameValue <apimNameValue>", "Apim Name Value", CommandOptionType.SingleValue);

            // list command options
            CommandOption configFile = this.Option("--configFile <configFile>", "Config YAML file location", CommandOptionType.SingleValue).IsRequired();

            // list command options
            CommandOption backendurlconfigFile = this.Option("--backendurlconfigFile <backendurlconfigFile>", "backend url json file location", CommandOptionType.SingleValue);

            // command options
            CommandOption preferredAPIsForDeployment = this.Option("--preferredAPIsForDeployment <preferredAPIsForDeployment>", "create ARM templates for the given APIs Name(comma separated) else leave this parameter blank then by default all api's will be considered", CommandOptionType.SingleValue);

            // Suffix command options
            CommandOption deploySuffix = this.Option("--deploySuffix <deploySuffix>", "append suffix to file name to not override ARM Log", CommandOptionType.SingleValue);

            CommandOption singleFile = this.Option("--singleFile", "generates target ARM resources in a single template file", CommandOptionType.NoValue);

            this.HelpOption();

            this.OnExecuteAsync(async (token) =>
            {
                // convert config file to CreatorConfig class
                FileReader fileReader = new FileReader();
                bool considerAllApiForDeployments = true;
                string[] preferredApis = null;

                GlobalConstants.CommandStartDateTime = DateTime.Now.ToString("MMyyyydd  hh mm ss");

                CreatorConfig creatorConfig = await fileReader.ConvertConfigYAMLToCreatorConfigAsync(configFile.Value());

                // do not produce linked templates
                // if a single file is requested

                if (singleFile.Values?.Count > 0)
                    creatorConfig.linked = false;

                if (apimNameValue != null && !string.IsNullOrEmpty(apimNameValue.Value()))
                {
                    creatorConfig.apimServiceName = apimNameValue.Value();
                }

                AppInsightsUpdater appInsightsUpdater = new AppInsightsUpdater();
                appInsightsUpdater.UpdateAppInsightNameAndInstrumentationKey(creatorConfig, appInsightsInstrumentationKey, appInsightsName);

                // Overwrite named values from build pipeline
                NamedValuesUpdater namedValuesUpdater = new NamedValuesUpdater();
                namedValuesUpdater.UpdateNamedValueInstances(creatorConfig, namedValueKeys);

                // validate creator config
                CreatorConfigurationValidator creatorConfigurationValidator = new CreatorConfigurationValidator(this);

                //if preferredAPIsForDeployment passed as parameter
                if (preferredAPIsForDeployment != null && !string.IsNullOrEmpty(preferredAPIsForDeployment.Value()))
                {
                    considerAllApiForDeployments = false;
                    preferredApis = preferredAPIsForDeployment.Value().Split(",");
                }

                //if backendurlfile passed as parameter
                if (backendurlconfigFile != null && !string.IsNullOrEmpty(backendurlconfigFile.Value()))
                {
                    CreatorApiBackendUrlUpdater creatorApiBackendUrlUpdater = new CreatorApiBackendUrlUpdater();
                    creatorConfig = creatorApiBackendUrlUpdater.UpdateBackendServiceUrl(backendurlconfigFile.Value(), creatorConfig);
                }

                bool isValidCreatorConfig = creatorConfigurationValidator.ValidateCreatorConfig(creatorConfig);
                if (isValidCreatorConfig == true)
                {
                    // required parameters have been supplied

                    // initialize file helper classes
                    FileWriter fileWriter = new FileWriter();
                    FileNameGenerator fileNameGenerator = new FileNameGenerator();
                    FileNames fileNames = creatorConfig.baseFileName == null ? fileNameGenerator.GenerateFileNames(creatorConfig.apimServiceName) : fileNameGenerator.GenerateFileNames(creatorConfig.baseFileName);

                    // initialize template creator classes
                    APIVersionSetTemplateCreator apiVersionSetTemplateCreator = new APIVersionSetTemplateCreator();
                    LoggerTemplateCreator loggerTemplateCreator = new LoggerTemplateCreator();
                    BackendTemplateCreator backendTemplateCreator = new BackendTemplateCreator();
                    AuthorizationServerTemplateCreator authorizationServerTemplateCreator = new AuthorizationServerTemplateCreator();
                    ProductAPITemplateCreator productAPITemplateCreator = new ProductAPITemplateCreator();
                    TagAPITemplateCreator tagAPITemplateCreator = new TagAPITemplateCreator();
                    PolicyTemplateCreator policyTemplateCreator = new PolicyTemplateCreator(fileReader);
                    ProductGroupTemplateCreator productGroupTemplateCreator = new ProductGroupTemplateCreator();
                    SubscriptionTemplateCreator productSubscriptionsTemplateCreator = new SubscriptionTemplateCreator();
                    GraphQLSchemaTemplateCreator graphQLSchemaTemplateCreator = new GraphQLSchemaTemplateCreator(fileReader);
                    DiagnosticTemplateCreator diagnosticTemplateCreator = new DiagnosticTemplateCreator();
                    ReleaseTemplateCreator releaseTemplateCreator = new ReleaseTemplateCreator();
                    ProductTemplateCreator productTemplateCreator = new ProductTemplateCreator(policyTemplateCreator, productGroupTemplateCreator, productSubscriptionsTemplateCreator);
                    PropertyTemplateCreator propertyTemplateCreator = new PropertyTemplateCreator();
                    TagTemplateCreator tagTemplateCreator = new TagTemplateCreator();
                    APITemplateCreator apiTemplateCreator = new APITemplateCreator(
                        fileReader,
                        policyTemplateCreator,
                        productAPITemplateCreator,
                        tagAPITemplateCreator,
                        graphQLSchemaTemplateCreator,
                        diagnosticTemplateCreator,
                        releaseTemplateCreator);

                    MasterTemplateCreator masterTemplateCreator = new MasterTemplateCreator(deploySuffix.Value());

                    // create templates from provided configuration
                    Console.WriteLine("Creating global service policy template");
                    Console.WriteLine("------------------------------------------");
                    Template globalServicePolicyTemplate = creatorConfig.policy != null ? policyTemplateCreator.CreateGlobalServicePolicyTemplate(creatorConfig) : null;
                    Console.WriteLine("Creating API version set template");
                    Console.WriteLine("------------------------------------------");
                    Template apiVersionSetsTemplate = creatorConfig.apiVersionSets != null ? apiVersionSetTemplateCreator.CreateAPIVersionSetTemplate(creatorConfig) : null;
                    Console.WriteLine("Creating product template");
                    Console.WriteLine("------------------------------------------");
                    Template productsTemplate = creatorConfig.products != null ? productTemplateCreator.CreateProductTemplate(creatorConfig) : null;
                    Console.WriteLine("Creating product/APIs template");
                    Console.WriteLine("------------------------------------------");
                    Template productAPIsTemplate = (creatorConfig.products != null || creatorConfig.apis != null) ? productAPITemplateCreator.CreateProductAPITemplate(creatorConfig) : null;
                    Console.WriteLine("Creating named values template");
                    Console.WriteLine("------------------------------------------");
                    Template propertyTemplate = creatorConfig.namedValues != null ? propertyTemplateCreator.CreatePropertyTemplate(creatorConfig) : null;
                    Console.WriteLine("Creating logger template");
                    Console.WriteLine("------------------------------------------");
                    Template loggersTemplate = creatorConfig.loggers != null ? loggerTemplateCreator.CreateLoggerTemplate(creatorConfig) : null;
                    Console.WriteLine("Creating backend template");
                    Console.WriteLine("------------------------------------------");
                    Template backendsTemplate = creatorConfig.backends != null ? backendTemplateCreator.CreateBackendTemplate(creatorConfig) : null;
                    Console.WriteLine("Creating authorization server template");
                    Console.WriteLine("------------------------------------------");
                    Template authorizationServersTemplate = creatorConfig.authorizationServers != null ? authorizationServerTemplateCreator.CreateAuthorizationServerTemplate(creatorConfig) : null;

                    // store name and whether the api will depend on the version set template each api necessary to build linked templates
                    List<LinkedMasterTemplateAPIInformation> apiInformation = new List<LinkedMasterTemplateAPIInformation>();
                    List<Template> apiTemplates = new List<Template>();
                    Console.WriteLine("Creating API templates");
                    Console.WriteLine("------------------------------------------");

                    IDictionary<string, string[]> apiVersions = APITemplateCreator.GetApiVersionSets(creatorConfig);
                    IList<string> splitApis = APITemplateCreator.GetSplittedAPI(creatorConfig);

                    foreach (APIConfig api in creatorConfig.apis)
                    {
                        if (considerAllApiForDeployments || preferredApis.Contains(api.name))
                        {
                            bool isServiceUrlParameterizeInYml = false;
                            if (creatorConfig.serviceUrlParameters != null && creatorConfig.serviceUrlParameters.Count > 0)
                            {
                                isServiceUrlParameterizeInYml = creatorConfig.serviceUrlParameters.Any(s => s.apiName.Equals(api.name));
                                api.serviceUrl = isServiceUrlParameterizeInYml ?
                                    creatorConfig.serviceUrlParameters.Where(s => s.apiName.Equals(api.name)).FirstOrDefault().serviceUrl : api.serviceUrl;
                            }
                            // create api templates from provided api config - if the api config contains a supplied apiVersion, split the templates into 2 for metadata and swagger content, otherwise create a unified template
                            List<Template> apiTemplateSet = await apiTemplateCreator.CreateAPITemplatesAsync(api, splitApis.Any(c => c == api.name));
                            apiTemplates.AddRange(apiTemplateSet);
                            // create the relevant info that will be needed to properly link to the api template(s) from the master template
                            /* Add only One link to the API Information
                            * an API can't change its name so :
                            */
                            if (!apiInformation.Any(apiI => apiI.name == api.name))
                                apiInformation.Add(new LinkedMasterTemplateAPIInformation()
                                {
                                    name = api.name,
                                    isSplit = splitApis.Any(c => c == api.name),
                                    dependsOnGlobalServicePolicies = creatorConfig.policy != null,
                                    dependsOnVersionSets = api.apiVersionSetId != null,
                                    dependsOnVersion = masterTemplateCreator.GetDependsOnPreviousApiVersion(api, apiVersions),
                                    dependsOnProducts = api.products != null,
                                    dependsOnTags = api.tags != null,
                                    dependsOnLoggers = await masterTemplateCreator.DetermineIfAPIDependsOnLoggerAsync(api, fileReader),
                                    dependsOnAuthorizationServers = api.authenticationSettings != null && api.authenticationSettings.oAuth2 != null && api.authenticationSettings.oAuth2.authorizationServerId != null,
                                    dependsOnBackends = await masterTemplateCreator.DetermineIfAPIDependsOnBackendAsync(api, fileReader),
                                    isServiceUrlParameterize = isServiceUrlParameterizeInYml,
                                    hasInitialRevisionOrVersion = creatorConfig.apis.Any(c => c.name == api.name && string.IsNullOrWhiteSpace(c.apiRevision)),
                                    hasRevision = creatorConfig.apis.Any(c => c.name == api.name && (c.apiRevision ?? "").Trim() != ""),
                                });
                        }
                    }

                    Console.WriteLine("Creating tag template");
                    Console.WriteLine("------------------------------------------");
                    Template tagTemplate = creatorConfig.tags != null ? tagTemplateCreator.CreateTagTemplate(creatorConfig) : null;

                    // create parameters file parameters to outputLocation
                    Template templateParameters = masterTemplateCreator.CreateMasterTemplateParameterValues(creatorConfig);
                    fileWriter.WriteJSONToFile(templateParameters, String.Concat(creatorConfig.outputLocation, fileNames.parameters));

                    // write templates to outputLocation
                    if (creatorConfig.linked == true)
                    {
                        // create linked master template
                        Template masterTemplate = masterTemplateCreator.CreateLinkedMasterTemplate(creatorConfig, globalServicePolicyTemplate, apiVersionSetsTemplate, productsTemplate, productAPIsTemplate, propertyTemplate, loggersTemplate, backendsTemplate, authorizationServersTemplate, tagTemplate, apiInformation, fileNames, creatorConfig.apimServiceName, fileNameGenerator);
                        fileWriter.WriteJSONToFile(masterTemplate, String.Concat(creatorConfig.outputLocation, fileNames.linkedMaster));
                    }
                    var templateToWrite = new Dictionary<string, Template>();
                    foreach (Template apiTemplate in apiTemplates)
                    {
                        APITemplateResource apiResource = apiTemplate.resources.FirstOrDefault(resource => resource.type == ResourceTypeConstants.API) as APITemplateResource;
                        APIConfig providedAPIConfiguration = creatorConfig.apis.FirstOrDefault(api => string.Compare(apiResource.name, APITemplateCreator.MakeResourceName(api), true) == 0);
                        // if the api version is not null the api is split into multiple templates. If the template is split and the content value has been set, then the template is for a subsequent api
                        string apiFileName = fileNameGenerator.GenerateCreatorAPIFileName(providedAPIConfiguration.name, splitApis.Any(c => c == providedAPIConfiguration.name), isInitialAPI: providedAPIConfiguration.apiRevision == null);
                        if (templateToWrite.ContainsKey(apiFileName))
                        {
                            var newResource = templateToWrite[apiFileName].resources.ToList();
                            foreach (var r in apiTemplate.resources)
                            {
                                if (newResource.Any(res => res.name == r.name))
                                    continue;
                                else
                                {
                                    newResource.Add(r);
                                    templateToWrite[apiFileName].resources = newResource.ToArray();
                                }

                            }
                        }
                        else
                            templateToWrite.Add(apiFileName, apiTemplate);
                    }

                    if (singleFile.Values?.Count > 0)
                    {
                        var targetTemplate = new TemplateCreator().CreateEmptyTemplate();
                        targetTemplate.parameters = new Dictionary<string, TemplateParameterProperties> {
                            { ParameterNames.ApimServiceName, new TemplateParameterProperties(){ type = "string" } },
                            { ParameterNames.LinkedTemplatesBaseUrl, new TemplateParameterProperties(){ type = "string", defaultValue = creatorConfig.linkedTemplatesBaseUrl } },
                            { ParameterNames.LinkedTemplatesUrlQueryString, new TemplateParameterProperties(){ type = "string", defaultValue = creatorConfig.linkedTemplatesUrlQueryString } },
                        };

                        if (globalServicePolicyTemplate != null && propertyTemplate?.resources?.Length > 0)
                        {
                            foreach (var globalServicePolicy in globalServicePolicyTemplate.resources)
                            {
                                // make global service policy depend on properties
                                globalServicePolicy.dependsOn = [
                                    .. globalServicePolicy.dependsOn,
                                    .. propertyTemplate.resources.Select(r => GetNamedValueResourceId(r))
                                    ];
                            }
                        }

                        foreach (var apiTemplate in templateToWrite.Values)
                        {
                            foreach (APITemplateResource apiResource in apiTemplate.resources.Where(r => r.type == MakeType("apis")))
                            {
                                var apiName = GetTypedResourceName(apiResource.name);
                                var rawApiName = GetRawApiName(apiName);
                                var parameter = apiTemplate.parameters.SingleOrDefault(p => p.Key.StartsWith(rawApiName));
                                if (parameter.Key != null)
                                    targetTemplate.parameters.AddOrUpdate(parameter.Key, parameter.Value);

                                // make apis depend on properties
                                if (propertyTemplate?.resources?.Length > 0)
                                    apiResource.dependsOn = apiResource.dependsOn.Concat(propertyTemplate.resources.Select(r => GetNamedValueResourceId(r)).ToArray()).ToArray();
                                // TODO: make apis depend on tags
                                // TODO: make apis depend on authorizationServers
                                // TODO: make apis depend on backends
                                // TODO: make apis depend on loggers

                                // make apis depend on their version sets
                                var apiVersionSetId = GetVersionSetResourceIdFromApiVersionSetId(apiResource);
                                if (apiVersionSetId != null && apiVersionSetsTemplate.resources.Any(r => GetTypedResourceName(r.name) == apiVersionSetId))
                                    apiResource.dependsOn = [
                                        .. apiResource.dependsOn,
                                        .. new[] { MakeApiVersionSetResourceId(apiVersionSetId), }
                                        ];
                            }
                        }

                        if (productsTemplate != null)
                        {
                            foreach (var productResource in productsTemplate.resources.Where(r => r.type == MakeType("products")))
                            {
                                // TODO: make products depend on tags
                                // TODO: make products depend on loggers
                            }

                            foreach (var productResource in productsTemplate.resources.Where(r => r.type == MakeType("products/policies")))
                            {
                                // make product policies depend on properties
                                if (propertyTemplate?.resources?.Length > 0)
                                    productResource.dependsOn = productResource.dependsOn.Concat(propertyTemplate.resources.Select(r => GetNamedValueResourceId(r)).ToArray()).ToArray();
                            }
                        }

                        if (productAPIsTemplate != null)
                        {
                            foreach (ProductAPITemplateResource productApiResource in productAPIsTemplate.resources)
                            {
                                var dependsOn = new List<string>();
                                var (productName, apiName) = GetProductAndApiFromProductApiAssociation(productApiResource);

                                if (productsTemplate != null)
                                {
                                    // make productApis depend on their products
                                    if (productName != null && productsTemplate.resources.Any(r => GetTypedResourceName(r.name) == productName))
                                        dependsOn.Add(MakeProductResourceId(productName));
                                }

                                // make productApis depend on their apis
                                if (apiName != null && templateToWrite.Values.SelectMany(r => r.resources).Any(r => r.type == MakeType("apis") && GetTypedResourceName(r.name) == apiName))
                                    dependsOn.Add(MakeApiResourceId(apiName));

                                productApiResource.dependsOn = [
                                    .. productApiResource.dependsOn,
                                    .. dependsOn
                                    ];
                            }
                        }

                        var templates = new List<Template> {
                            tagTemplate,
                            loggersTemplate,
                            backendsTemplate,
                            authorizationServersTemplate,

                            propertyTemplate,
                            apiVersionSetsTemplate,
                            globalServicePolicyTemplate,
                        };

                        templates.AddRange(templateToWrite.Values);
                        templates.AddRange(new[] {
                            productsTemplate,
                            productAPIsTemplate,
                        });

                        foreach (var template in templates)
                        {
                            if (template != null && template.resources.Length > 0)
                                targetTemplate.resources = [
                                    .. targetTemplate.resources,
                                    .. template.resources
                                    ];
                        }

                        fileWriter.WriteJSONToFile(targetTemplate, String.Concat(creatorConfig.outputLocation, fileNames.linkedMaster));
                    }

                    // write each ARM template in its own file

                    else
                    {
                        foreach (var item in templateToWrite)
                        {
                            fileWriter.WriteJSONToFile(item.Value, String.Concat(creatorConfig.outputLocation, item.Key));
                        }
                        if (globalServicePolicyTemplate != null)
                        {
                            fileWriter.WriteJSONToFile(globalServicePolicyTemplate, String.Concat(creatorConfig.outputLocation, fileNames.globalServicePolicy));
                        }
                        if (apiVersionSetsTemplate != null)
                        {
                            fileWriter.WriteJSONToFile(apiVersionSetsTemplate, String.Concat(creatorConfig.outputLocation, fileNames.apiVersionSets));
                        }
                        if (productsTemplate != null)
                        {
                            fileWriter.WriteJSONToFile(productsTemplate, String.Concat(creatorConfig.outputLocation, fileNames.products));
                        }
                        if (productAPIsTemplate != null)
                        {
                            fileWriter.WriteJSONToFile(productAPIsTemplate, String.Concat(creatorConfig.outputLocation, fileNames.productAPIs));
                        }
                        if (propertyTemplate != null)
                        {
                            fileWriter.WriteJSONToFile(propertyTemplate, String.Concat(creatorConfig.outputLocation, fileNames.namedValues));
                        }
                        if (loggersTemplate != null)
                        {
                            fileWriter.WriteJSONToFile(loggersTemplate, String.Concat(creatorConfig.outputLocation, fileNames.loggers));
                        }
                        if (backendsTemplate != null)
                        {
                            fileWriter.WriteJSONToFile(backendsTemplate, String.Concat(creatorConfig.outputLocation, fileNames.backends));
                        }
                        if (authorizationServersTemplate != null)
                        {
                            fileWriter.WriteJSONToFile(authorizationServersTemplate, String.Concat(creatorConfig.outputLocation, fileNames.authorizationServers));
                        }
                        if (tagTemplate != null)
                        {
                            fileWriter.WriteJSONToFile(tagTemplate, String.Concat(creatorConfig.outputLocation, fileNames.tags));
                        }
                    }

                    Console.WriteLine("Templates written to output location");
                }
                return 0;
            });
        }

        private (string product, string api) GetProductAndApiFromProductApiAssociation(ProductAPITemplateResource resource)
        {
            var regex = new Regex(@"\[concat\(parameters\('ApimServiceName'\), '/(?<p>[^\\]+)/(?<a>[^']+)'\)\]");
            var match = regex.Match(resource.name);
            if (match.Success)
                return (match.Groups["p"].Value, match.Groups["a"].Value);

            throw new NotSupportedException();
        }

        private string? GetVersionSetResourceIdFromApiVersionSetId(APITemplateResource resource)
        {
            var regex = new Regex(@"\[resourceId\('Microsoft.ApiManagement/service/apiVersionSets', parameters\('ApimServiceName'\), '(?<n>[^']+)'\)\]");
            var match = regex.Match(resource.properties.apiVersionSetId ?? "");
            if (match.Success)
            {
                var versionSetId = match.Groups["n"].Value;
                return versionSetId;
            }

            return null;
        }

        private string GetNamedValueResourceId(TemplateResource resource)
        {
            var regex = new Regex(@"\[concat\(parameters\('ApimServiceName'\), '/(?<n>[^']+)'\)\]");
            var match = regex.Match(resource.name);
            if (match.Success)
            {
                var name = match.Groups["n"].Value;
                return MakeNamedValueResourceId(name);
            }

            throw new NotSupportedException();
        }
        private string MakeApiResourceId(string name)
            => MakeTypedResourceId("apis", name);
        private string MakeApiVersionSetResourceId(string apiVersionSetId)
            => MakeTypedResourceId("apiVersionSets", apiVersionSetId);
        private static string MakeNamedValueResourceId(string name)
            => MakeTypedResourceId("namedValues", name);
        private static string MakeProductResourceId(string name)
            => MakeTypedResourceId("products", name);
        private static string MakeTypedResourceId(string type, string name)
            => $"[resourceId('{MakeType(type)}', parameters('ApimServiceName'), '{name}')]";
        private static string MakeType(string type)
            => $"Microsoft.ApiManagement/service/{type}";

        private static string GetTypedResourceName(string name)
        {
            var regex = new Regex(@"\[concat\(parameters\('ApimServiceName'\), '/(?<n>[^\\]+)'\)\]");
            var match = regex.Match(name);
            if (match.Success)
                return match.Groups["n"].Value;

            throw new NotSupportedException();
        }

        private static string GetRawApiName(string name)
            => new Regex(";rev=.+").Replace(name, "");
    }

    internal static class DictionaryExtensions
    {
        public static void AddOrUpdate<TKey, TValue>(this IDictionary<TKey, TValue> dictionary, TKey key, TValue value)
        {
            if (dictionary.ContainsKey(key))
                dictionary[key] = value;
            else
                dictionary.Add(key, value);
        }
    }
}

using Microsoft.Azure.Management.ApiManagement.ArmTemplates.Common;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using YamlDotNet.Serialization;

namespace apimtemplate.Yaml
{
    public class TemplateType
    {
       public const string api = "Microsoft.ApiManagement/service/apis";
       public const string operations = "Microsoft.ApiManagement/service/apis/operations";
       public const string oprationsPolicy = "Microsoft.ApiManagement/service/apis/operations/policies";
       public const string schemas = "Microsoft.ApiManagement/service/apis/schemas";
       public const string tags = "Microsoft.ApiManagement/service/apis/operations/tags";
       public const string producs = "Microsoft.ApiManagement/service/products/apis";
       public const string apidiagnostics = "Microsoft.ApiManagement/service/apis/diagnostics";
       public const string policies = "Microsoft.ApiManagement/service/apis/policies";
       public const string diagnostics = "Microsoft.ApiManagement/service/diagnostics";
    }

    public class ExctractedSubscription
    {
        public string id { get; set; }
        public string productId { get; set; }
        public string name { get; set; }
        public string primaryKey { get; set; }
        public string secondaryKey { get; set; }
    }

    public class ExtractedNamedValue
    {
        public string id { get; set; }
        public string value { get; set; }
        public string name { get; set; }
        public bool secret { get; set; }
    }

    public class YamlExtractorUtils
    {

        static Dictionary<string, APIVersionSetProperties> VersionSets;

        static Dictionary<string, PolicyTemplateProperties> AllOperationByName;
        static Dictionary<string, Dictionary<string, OperationTemplateProperties>> OperationsByName;
        static Dictionary<string, Dictionary<string, PolicyTemplateProperties>> OperationPolicyByName;

        static Dictionary<String, PolicyTemplateProperties> ProductPolicyByName;

        static Dictionary<string, List<string>> ProductByApi;

        static Dictionary<string, List<ExctractedSubscription>> ExtractedSubscriptionKey = new Dictionary<string, List<ExctractedSubscription>>();

        static string BasePath;

        public static void Extract(YamlExtractorConfig config)
        {
            //Init 
            BasePath = Path.Combine("Helm");

            var initPath = @"C:\Temp\apim-extract\";
            var apimName = "";

            initPath = config.initPath;
            apimName = config.apimName;

            var productKeyPath = @"keys.json";
            var namedValuesKeyPath = @"namedValues.json";

            productKeyPath = Path.Combine(initPath, "keys.json");
            namedValuesKeyPath = Path.Combine(initPath, "namedValues.json");


            //initPath = @"C:\Temp\ilyass2";
            //apimName = "apim-lplcloud-prd";

            var templatePath = Path.Combine(initPath, $"{apimName}-apis.template.json");
            var versionSetPath = Path.Combine(initPath, $"{apimName}-apiVersionSets.template.json");
            var productPath = Path.Combine(initPath, $"{apimName}-products.template.json");
            var productApiPath = Path.Combine(initPath, $"{apimName}-productAPIs.template.json");
            var namedValuesPath = Path.Combine(initPath, $"{apimName}-namedValues.template.json");


            GetExtractedKey(productKeyPath);
            ExtractVersionSet(versionSetPath);
            ExtractProductAPIS(productApiPath);
            ExtractNamedValues(namedValuesPath, namedValuesKeyPath);


            var jsonString = System.IO.File.ReadAllText(templatePath);
                
            var apiTemplate = Newtonsoft.Json.JsonConvert.DeserializeObject<Template>(jsonString);
            var group = apiTemplate.resources.GroupBy(r => r.type);

            var apis  = group.First(r => r.Key == TemplateType.api).ToList();
            var globalPolicy = group.First(r => r.Key == TemplateType.policies).ToList();
            var operations = group.First(r => r.Key == TemplateType.operations).ToList();
            var operationsPolicies = group.FirstOrDefault(r => r.Key == TemplateType.oprationsPolicy)?.ToList();
            if (operationsPolicies == null)
                operationsPolicies = new List<TemplateResource>();
            


            System.IO.Directory.CreateDirectory("apis");
            //Extract All Operations Policies.
            AllOperationByName = ExtractAllOperation(globalPolicy);
            OperationsByName = ExtractOperation(operations);
            OperationPolicyByName = ExtractOperationPolicy(operationsPolicies);

            if(File.Exists(productPath))
            {
                var productJsonString = System.IO.File.ReadAllText(productPath);
                var productTemplate = JsonConvert.DeserializeObject<Template>(productJsonString);
                ExtractProduct(productTemplate);
            }


            ExtractApi(apis);
        }

        private static void ExtractNamedValues(string path , string keyvaluespath)
        {
            var serializer = new SerializerBuilder()
                 //.WithNamingConvention(CamelCaseNamingConvention.Instance)
                 .Build();

            var namedValuesExtracted = new Dictionary<string, ExtractedNamedValue>();

            if(!String.IsNullOrEmpty(keyvaluespath))
            {
                if(File.Exists(keyvaluespath))
                {
                    var keyValuesJson = System.IO.File.ReadAllText(keyvaluespath);
                    var keyValues = JsonConvert.DeserializeObject<List<ExtractedNamedValue>>(keyValuesJson);

                    foreach (var k in keyValues)
                        if(!namedValuesExtracted.ContainsKey(k.id))
                            namedValuesExtracted.Add(k.id, k);
                }
                
            }
            


            Console.WriteLine("===========================================");
            Console.WriteLine("==========  Named Values  =================");
            Console.WriteLine("===========================================");
            if (!File.Exists(path))
            {
                Console.WriteLine("No Name Values Found");
                return;
            }

            var templateString = System.IO.File.ReadAllText(path);
            var template = JsonConvert.DeserializeObject<Template>(templateString);

            var dict = new Dictionary<string, PropertyResourceProperties>();
            foreach(var resource in template.resources)
            {
                var name = resource.name.Remove(0, "[concat(parameters('ApimServiceName'), '/".Length).Replace("')]", "");
                var props = resource.properties.ToObject<PropertyResourceProperties>();
                dict.Add(name, props);
            }

            var namedValues = new List<Dictionary<string, object>>();            

            foreach(var namedValue in dict)
            {
                var result = new Dictionary<string, object>();
                result.Add("displayName", namedValue.Value.displayName);
                result.Add("name", namedValue.Key);
                if(namedValue.Value.secret == false)
                {
                    result.Add("secret", namedValue.Value.secret);
                    result.Add("value", namedValue.Value.value);
                }
                else if(namedValue.Value.keyVault != null)
                {
                    result.Add("secret", true);
                    result.Add("keyVault", new
                    {
                        secretIdentifier = namedValue.Value.keyVault.secretIdentifier
                    });
                }
                else
                {
                    result.Add("secret", true);
                    if (namedValuesExtracted.ContainsKey(namedValue.Key))
                        result.Add("value", namedValuesExtracted[namedValue.Key].value);
                    else
                        result.Add("value", namedValue.Value.value);
                }

                namedValues.Add(result);
            }

            Console.WriteLine($" Found {dict.Count} NamedValues");
            Console.WriteLine($" Found {namedValues.Where(d => ((Boolean)d["secret"]) == false).Count()} in clear Text");
            Console.WriteLine($" Found {namedValues.Where(d => ((Boolean)d["secret"]) == true && d.ContainsKey("keyVault")).Count()} with KeyVault");
            Console.WriteLine($" Found {namedValues.Where(d => ((Boolean)d["secret"]) == true && !d.ContainsKey("keyVault") && !String.IsNullOrEmpty((string)d["value"])).Count()} extracted values");
            Console.WriteLine($" Found {namedValues.Where(d => ((Boolean)d["secret"]) == true && !d.ContainsKey("keyVault") && String.IsNullOrEmpty((string)d["value"]) ).Count()} missing values");
            
            Console.WriteLine(" End NamedValues");
            Console.WriteLine("===========================================");

            var s = serializer.Serialize(new { namedValue = namedValues });
            Directory.CreateDirectory(BasePath);
            File.WriteAllText(Path.Combine(BasePath, "namedValues.yaml"), s);
        }

        private static void GetExtractedKey(string path)
        {
            var jsonString = System.IO.File.ReadAllText(path);
            var keys = JsonConvert.DeserializeObject<ExctractedSubscription[]>(jsonString);

            foreach (var key in keys)
                if (key.productId != null)//id can be null, ignore
                {
                    if (!ExtractedSubscriptionKey.ContainsKey(key.productId))
                        ExtractedSubscriptionKey.Add(key.productId, new List<ExctractedSubscription>());

                    ExtractedSubscriptionKey[key.productId].Add(key);
                }
            
        }

        private static void ExtractProduct(Template productTemplate)
        {
            ProductPolicyByName = new Dictionary<string, PolicyTemplateProperties>();

            var group = productTemplate.resources.GroupBy(r => r.type);

            var products = group.First(r => r.Key == ResourceTypeConstants.Product);
            var productGroups = group.First(r => r.Key == ResourceTypeConstants.ProductGroup);
            var productPolicies = group.First(r => r.Key == ResourceTypeConstants.ProductPolicy);

            foreach(var productPolicy in productPolicies)
            {
                var productName = productPolicy.name.Remove(0, "[concat(parameters('ApimServiceName'), '/".Length).Replace("/policy')]", "");
                var policy = productPolicy.properties.ToObject<PolicyTemplateProperties>();
                ProductPolicyByName.Add(productName, policy);
            }


            foreach(var product in products)
            {
                var productName = product.name.Remove(0, "[concat(parameters('ApimServiceName'), '/".Length).Replace("')]", "");
                var props = product.properties.ToObject<ProductsTemplateProperties>();

                CreateProduct(productName, props);
            }
        }

        private static void CreateProduct(string productName, ProductsTemplateProperties product)
        {
            var pDirectory = Path.Combine(BasePath, "ApiManagement", "Products");
            Directory.CreateDirectory(pDirectory);

            var serializer = new SerializerBuilder()
                    //.WithNamingConvention(CamelCaseNamingConvention.Instance)
                    .Build();

            var directory = Path.Combine(BasePath,"configuration","products", productName);
            Directory.CreateDirectory(directory);
            var o = new
            {
                product = new Dictionary<string,object >()
                {
                    { "displayName" , product.displayName },
                    { "description" , product.description },
                    {"subscriptionRequired" , product.subscriptionRequired }
                }
            };

            if(ExtractedSubscriptionKey.ContainsKey(productName))
            {
                var subs = ExtractedSubscriptionKey[productName];
                o.product.Add("subscriptions", new List<object>(
                    subs.Select(s => new {
                        name = String.IsNullOrEmpty(s.id) ? s.productId:s.id,
                        primaryKey = s.primaryKey,
                        secondaryKey = s.secondaryKey,
                    })
                    )
                );
            }
                

            var productYamlObject = new Dictionary<string, object>(){
                            { productName, o } };

            var s = serializer.Serialize(productYamlObject);

            if(ProductPolicyByName.ContainsKey(productName))
            {
                var policy = ProductPolicyByName[productName];
                var policyPath = Path.Combine(pDirectory, productName);
                Directory.CreateDirectory(policyPath);
                File.WriteAllText(Path.Combine(policyPath,"policy.xml"), policy.value);
            }

            var valuesFiles = Path.Combine(directory, "values.yaml");
            File.WriteAllText(valuesFiles, s);
        }

        public static void ExtractProductAPIS(string path)
        {
            if (!File.Exists(path))
                return;
            var jsonString = System.IO.File.ReadAllText(path);

            var productApi = JsonConvert.DeserializeObject<Template>(jsonString);
            var productApiResources = productApi.resources;

            ProductByApi = new Dictionary<string, List<string>>();

            foreach (var productApiResource in productApiResources)
            {
                var apiProduct = productApiResource.name.Remove(0, "[concat(parameters('ApimServiceName'), '/".Length).Replace("')]", "").Split("/");
                var productName = apiProduct[0];
                var apiName = apiProduct[1];

                if (!ProductByApi.ContainsKey(apiName))
                    ProductByApi.Add(apiName, new List<string>());

                ProductByApi[apiName].Add(productName);
            }
        }

        public static void ExtractVersionSet(string path)
        {
            if (!File.Exists(path))
                return;

            var jsonString = System.IO.File.ReadAllText(path);

            var versionSetTemplate = JsonConvert.DeserializeObject<Template>(jsonString);
            var versionSets = versionSetTemplate.resources;

            VersionSets = new Dictionary<string, APIVersionSetProperties>();

            foreach (var versionSet in versionSets)
            {
                var name = versionSet.name.Remove(0, "[concat(parameters('ApimServiceName'), '/".Length).Replace("')]","");
                var props = versionSet.properties.ToObject<APIVersionSetProperties>();

                VersionSets.Add(name, props);
            }
        }

        public static Dictionary<string, PolicyTemplateProperties> ExtractAllOperation(IEnumerable<TemplateResource> policies)
        {
            var result = new Dictionary<string, PolicyTemplateProperties>();
            foreach(var policy in policies)
            {
                var apiName = policy.name.Remove(0, "[concat(parameters('ApimServiceName'), '/".Length).Replace("/policy')]", "");
                var props = policy.properties.ToObject<PolicyTemplateProperties>();
                result.Add(apiName,props);
            }
            return result;
        }

        public static Dictionary<string, Dictionary<string, PolicyTemplateProperties>> ExtractOperationPolicy(IEnumerable<TemplateResource> operations)
        {
            //Policy depend on Operations
            var operationPolicyByName = new Dictionary<string, Dictionary<string, PolicyTemplateProperties>>();
            //Operations depend on ApiName and Schema
            foreach (var operation in operations)
            {
                var dependsOn = operation.dependsOn.First(o => o.StartsWith("[resourceId('Microsoft.ApiManagement/service/apis/operations', parameters('ApimServiceName'),"));
                var apiNameAndOperations = dependsOn.Remove(0,"[resourceId('Microsoft.ApiManagement/service/apis/operations', parameters('ApimServiceName'),".Length).Replace("')]", "");
                var apiNameAndOperationsArray = apiNameAndOperations.Split(",").Select(s => s.Replace("'","").Trim()).ToArray();
                var apiName = apiNameAndOperationsArray[0];
                var operationName = apiNameAndOperationsArray[1];

                var props = operation.properties.ToObject<PolicyTemplateProperties>();

                if (!operationPolicyByName.ContainsKey(apiName))
                    operationPolicyByName.Add(apiName, new Dictionary<string, PolicyTemplateProperties>());

                operationPolicyByName[apiName].Add(operationName, props);
                
            }

            return operationPolicyByName;
        }

        public static Dictionary<string, Dictionary<string, OperationTemplateProperties>> ExtractOperation(IEnumerable<TemplateResource> operations)
        {
            var operationByName = new Dictionary<string, Dictionary<string, OperationTemplateProperties>>();
            //Operations depend on ApiName and Schema
            foreach (var operation in operations)
            {
                var dependsOn = operation.dependsOn.First(o => o.StartsWith("[resourceId('Microsoft.ApiManagement/service/apis', parameters('ApimServiceName'),"));
                var apiName = dependsOn.Remove(0,"[resourceId('Microsoft.ApiManagement/service/apis', parameters('ApimServiceName'),".Length).Replace("')]", "");
                var props = operation.properties.ToObject<OperationTemplateProperties>();

                var apiNameAndOperations = operation.name.Remove(0, "[concat(parameters('ApimServiceName'), '/".Length).Replace("')]", "").Split("/");
                var operationName = apiNameAndOperations[1];


                if (!operationByName.ContainsKey(apiName))
                    operationByName.Add(apiName, new Dictionary<string, OperationTemplateProperties>());

                operationByName[apiName].Add(operationName, props);
                
            }

            return operationByName;
        }

        public static void ExtractApi(IEnumerable<TemplateResource> apis)
        {
            var apiWithRev = new Dictionary<string, List<APITemplateProperties>> ();
            foreach(var api in apis.OrderBy(o=>o.name))
            {
                var name = api.name.Remove(0, "[concat(parameters('ApimServiceName'), '/".Length).Replace("')]","");
                var props = api.properties.ToObject<APITemplateProperties>();

                if (name.Contains(";rev="))
                {
                    //this is a revision
                    var subName = name.Remove(name.IndexOf(";"));
                    apiWithRev[subName].Add(props);
                }
                else
                {
                    Directory.CreateDirectory(Path.Combine("apis", name));
                    apiWithRev.Add(name, new List<APITemplateProperties>() { props });
                }
            }

            foreach(var api in apiWithRev)
            {
                var name = api.Key;
                var props = api.Value;
                Console.WriteLine($"{name}");
                CreateApis(name, props);
            }
        }

        public static void CreateApis(string name, List<APITemplateProperties> properties)
        {
            var serializer = new SerializerBuilder()
                   //.WithNamingConvention(CamelCaseNamingConvention.Instance)
                   .Build();
            var directory = Path.Combine(BasePath, "configuration", "apis", name);
            Dictionary<string, Dictionary<string, object>> result = null;
            foreach (var props in properties)
            {
                
                if(result == null)
                {
                    var o = CreateApi(name, props, false);
                    result = o;
                }
                else
                {
                    var o = CreateApi(name, props, true);
                    var a = result[name];
                    var api = a["api"];
                    var apiDic = api as Dictionary<string, object>;
                    if (!apiDic.ContainsKey("revisions"))
                        apiDic.Add("revisions", new List<object>());

                    var apiYamlObject = (new Dictionary<string, object>()
                    {
                        {"apiRevision", props.apiRevision },
                    
                    });

                    apiYamlObject.CreateDictionaryElementIfExist("isCurrent", props.isCurrent);
                    apiYamlObject.CreateDictionaryElementIfExist("apiRevisionDescription", props.apiRevisionDescription);
                    (apiDic["revisions"] as List<Object>).Add(apiYamlObject);

                    
                }

            }

            var s = serializer.Serialize(result);
            var valuesFiles = Path.Combine(directory, "values.yaml");
            File.WriteAllText(valuesFiles, s);
        }

        public static Dictionary<string, Dictionary<string, object>> CreateApi(string apiName, APITemplateProperties properties, bool isRevision)
        {
            var directory = Path.Combine(BasePath, "configuration", "apis", apiName);
            var baseVersionSetPath = Path.Combine(BasePath, "configuration", "apiversion-sets");
            
            Directory.CreateDirectory(directory);
            Directory.CreateDirectory(baseVersionSetPath);

            var serializer = new SerializerBuilder()
                    //.WithNamingConvention(CamelCaseNamingConvention.Instance)
                    .Build();

            var apiYamlObject = new Dictionary<string, object>(){
                            { "name", apiName } };

            apiYamlObject.CreateDictionaryElementIfExist("displayName", properties.displayName);
            apiYamlObject.CreateDictionaryElementIfExist("description", properties.description);
            apiYamlObject.CreateDictionaryElementIfExist("serviceUrl", properties.serviceUrl);
            apiYamlObject.CreateDictionaryElementIfExist("isCurrent", properties.isCurrent);
            apiYamlObject.CreateDictionaryElementIfExist("apiVersion", properties.apiVersion);
            apiYamlObject.CreateDictionaryElementIfExist("apiVersionDescription", properties.apiVersionDescription);
            apiYamlObject.CreateDictionaryElementIfExist("apiRevision", properties.apiRevision);
            apiYamlObject.CreateDictionaryElementIfExist("apiRevisionDescription", properties.apiRevisionDescription);
            apiYamlObject.CreateDictionaryElementIfExist("subscriptionRequired", properties.subscriptionRequired);
           
            var o = new Dictionary<string, Dictionary<string,object>>()
            {
                {
                    apiName, new Dictionary<String,object>(){
                        { "api" , apiYamlObject }
                    }
                },
            };

            //Extract all operations 
            if(AllOperationByName.ContainsKey(apiName))
            {
                var apiNameAllOperationPolicy = AllOperationByName[apiName];
                if (apiNameAllOperationPolicy.format =="rawxml")
                {
                    var path = Path.Combine(BasePath, "ApiManagement", "Apis", apiName, "policies");
                    if (isRevision)
                        path = Path.Combine(path, "revisions", properties.apiRevision); 
                    
                    Directory.CreateDirectory(path);
                    var policyLink = Path.Combine(path, $"all-operations.xml");
                    File.WriteAllText(policyLink, apiNameAllOperationPolicy.value);
                    apiYamlObject.Add("policy", policyLink);
                }
            }

            //GetOperations Policy
            if(OperationPolicyByName.ContainsKey(apiName))
            {
                var operations = new Dictionary<string, object>();

                foreach(var op in OperationPolicyByName[apiName])
                {
                    string policyLink = null;
                    if(op.Value.format == "rawxml")
                    {
                        var path = Path.Combine(BasePath, "ApiManagement", "Apis", apiName, "policies", "operations");

                        Directory.CreateDirectory(path);
                        policyLink = Path.Combine(path, $"{op.Key}.policy.xml");
                        File.WriteAllText(policyLink, op.Value.value);
                    }

                    operations.Add(op.Key, new { policy = policyLink });
                }

                apiYamlObject.Add("operations", operations);
            }


            //VersionSetId
            if(!String.IsNullOrEmpty(properties.apiVersionSetId))
            {
                var apiVersionSetId = properties.apiVersionSetId.Remove(0, "[concat(resourceId('Microsoft.ApiManagement/service', parameters('ApimServiceName')), '/apiVersionSets/".Length).Replace("')]", "");


                var versionSet = VersionSets[apiVersionSetId];

                var versionSetObject = new
                {
                    apiVersionSets = new
                    {
                        id = apiVersionSetId,
                        displayName = versionSet.displayName,
                        description = versionSet.description,
                        versioninScheme = versionSet.versioningScheme,

                    }
                };
                var versionSetYaml = serializer.Serialize(versionSetObject);

                //Check if version Set already exist
                if(!File.Exists(Path.Combine(baseVersionSetPath, $"{apiVersionSetId}.yaml")))
                {
                    var versionSetPath = Path.Combine(baseVersionSetPath, $"{apiVersionSetId}.yaml");
                    File.WriteAllText(versionSetPath, versionSetYaml);

                }

                apiYamlObject.Add("apiVersionSetId", apiVersionSetId);
            }

            //Products
            if(ProductByApi.ContainsKey(apiName))
            {
                var products = new
                {
                    use = ProductByApi[apiName].ToArray()
                };
                var productsYaml = serializer.Serialize(products);
                var productsPath = Path.Combine(directory, "products.yaml");
                File.WriteAllText(productsPath, productsYaml);
            }


            return o;
            
        }

        
    }

    public static class YamlExtensions
    {
        public static void CreateDictionaryElementIfExist(this Dictionary<string, object> dico, string propertyName, object propertyValue)
        {
            if (propertyValue != null)
            {
                dico.Add(propertyName, propertyValue);
            }
        }
    }
}

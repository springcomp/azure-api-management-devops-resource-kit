using Microsoft.Azure.Management.ApiManagement.ArmTemplates.Common;
using Microsoft.Azure.Management.ApiManagement.ArmTemplates.Extract;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;

namespace apimtemplate.Yaml
{
    public static class Helpers
    {

        public static string GetProductName(string productName, out bool isEnvAvailable)
        {
            return GetApiName(productName,out isEnvAvailable);
        }
     
        public static string GetApiName(string sourceApiName, out bool isEnvAvailable)
        {
            isEnvAvailable = false;
            var index = sourceApiName.IndexOf("-");
            
            if (index != -1)
            {
                isEnvAvailable = true;
                var apiName = sourceApiName.Remove(0, index + 1);
                return apiName;
            }
            return sourceApiName;
        }

        public static string GetEnvName(string sourceApiName)
        {
            var index = sourceApiName.IndexOf("-");
            if(index != -1)
                return sourceApiName.Remove(index);
            return String.Empty;
        }

        public static string GetResourceFileName(string apiName, string prefix, string suffix)
        {
            var env = Helpers.GetEnvName(apiName);
            var fileNamePart = new List<string>();
            fileNamePart.Add(prefix);
            if (!String.IsNullOrEmpty(env))
                fileNamePart.Add(env);
            fileNamePart.Add(suffix);

            return String.Join(".", fileNamePart.ToArray());
        }

        public static IList<T> GetTemplateResourceOrEmptyList<T>(this IEnumerable<IGrouping<string,T>> enumerable, string key)
        {
            var f =  enumerable.FirstOrDefault(r => r.Key == key)?.ToList();
            if (f == null)
                f = new List<T>();
            return f;
        }

        public static string RemovePrefixARMResource(this string resourceName)
        {
            return resourceName.Remove(0, "[concat(parameters('ApimServiceName'), '/".Length);
        }

        public static async Task<string> GetNamedValueFromApi(EntityExtractor entityExtractor, string namedValueId, string resourceGroupName, string apiManagementName)
        {
            //POST https://management.azure.com/subscriptions/{subscriptionId}/resourceGroups/{resourceGroupName}/providers/Microsoft.ApiManagement/service/{serviceName}/namedValues/{namedValueId}/listValue?api-version=2021-08-01

            (string azToken, string azSubId) = await entityExtractor.auth.GetAccessToken();
            string requestUrl = string.Format("{0}/subscriptions/{1}/resourceGroups/{2}/providers/Microsoft.ApiManagement/service/{3}/namedValues/{4}/listValue?api-version={5}",
               EntityExtractor.baseUrl, azSubId, resourceGroupName, apiManagementName, namedValueId, GlobalConstants.APIVersion);

            return await EntityExtractor.CallApiManagementAsync(azToken, requestUrl, HttpMethod.Post);
        }

        public  static async Task<IList<SubscriptionsTemplateResource>> GetSubscriptionFromProduct(EntityExtractor entityExtractor, string productId, string resourceGroupName, string apiManagementName)
        {
            //GET https://management.azure.com/subscriptions/{subscriptionId}/resourceGroups/{resourceGroupName}/providers/Microsoft.ApiManagement/service/{serviceName}/subscriptions?$filter={$filter}&$top={$top}&$skip={$skip}&api-version=2021-08-01

            (string azToken, string azSubId) = await entityExtractor.auth.GetAccessToken();
            string requestUrl = string.Format("{0}/subscriptions/{1}/resourceGroups/{2}/providers/Microsoft.ApiManagement/service/{3}/subscriptions/?$filter=scope eq '/products/{4}'&api-version={5}",
               EntityExtractor.baseUrl, azSubId, resourceGroupName, apiManagementName, productId, "2021-08-01");

            var subJson = await EntityExtractor.CallApiManagementAsync(azToken, requestUrl);
            return Newtonsoft.Json.Linq.JObject.Parse(subJson).GetValue("value").ToObject<List<SubscriptionsTemplateResource>>();
        }

        public static async Task<string> GetSubscriptionSecretFromApi(EntityExtractor entityExtractor, string subscriptionId, string resourceGroupName, string apiManagementName)
        {
            //POST https://management.azure.com/subscriptions/{subscriptionId}/resourceGroups/{resourceGroupName}/providers/Microsoft.ApiManagement/service/{serviceName}/subscriptions/{sid}/listSecrets?api-version=2021-08-01

            (string azToken, string azSubId) = await entityExtractor.auth.GetAccessToken();
            string requestUrl = string.Format("{0}/subscriptions/{1}/resourceGroups/{2}/providers/Microsoft.ApiManagement/service/{3}/subscriptions/{4}/listSecrets?api-version={5}",
               EntityExtractor.baseUrl, azSubId, resourceGroupName, apiManagementName, subscriptionId, "2021-08-01");

            return await EntityExtractor.CallApiManagementAsync(azToken, requestUrl, HttpMethod.Post);
        }

        public static async Task<string> GetSwaggerUrl(EntityExtractor entityExtractor, string apiId, string resourceGroupName, string apiManagementName)
        {
            //https://docs.microsoft.com/en-us/rest/api/apimanagement/current-ga/api-export/get#exportformat
            var exportFormat = "openapi+json-link";

            //GET https://management.azure.com/subscriptions/{subscriptionId}/resourceGroups/{resourceGroupName}/providers/Microsoft.ApiManagement/service/{serviceName}/apis/{apiId}?format={format}&export=true&api-version=2021-08-01
            (string azToken, string azSubId) = await entityExtractor.auth.GetAccessToken();
            string requestUrl = string.Format("{0}/subscriptions/{1}/resourceGroups/{2}/providers/Microsoft.ApiManagement/service/{3}/apis/{4}?format={5}&export=true&api-version={6}",
               EntityExtractor.baseUrl, azSubId, resourceGroupName, apiManagementName, apiId, exportFormat, "2021-08-01");

            var subJson = await EntityExtractor.CallApiManagementAsync(azToken, requestUrl);
            return subJson;
        }
    }
}

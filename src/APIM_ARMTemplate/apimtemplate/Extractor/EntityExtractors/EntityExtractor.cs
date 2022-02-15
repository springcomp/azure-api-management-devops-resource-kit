using System.Collections.Generic;
using System.Net.Http;
using System;
using System.Net.Http.Headers;
using System.Threading.Tasks;
using Microsoft.Azure.Management.ApiManagement.ArmTemplates.Common;
using Microsoft.Azure.Management.ApiManagement.ArmTemplates.Create;
using Microsoft.Extensions.Caching.Memory;
using Newtonsoft.Json.Linq;

namespace Microsoft.Azure.Management.ApiManagement.ArmTemplates.Extract
{
    public class EntityExtractor
    {
        public static string baseUrl = "https://management.azure.com";
        internal Authentication auth = new Authentication();
        private static readonly IMemoryCache _cache = new MemoryCache(new MemoryCacheOptions());
        private static HttpClient httpClient = new HttpClient();

        public static async Task<string> CallApiManagementAsync(string azToken, string requestUrl)
        {
            return await CallApiManagementAsync(azToken, requestUrl, HttpMethod.Get);
        }
        public static async Task<string> CallApiManagementAsync(string azToken, string requestUrl, HttpMethod method)
        {
            if (_cache.TryGetValue(requestUrl, out string cachedResponseBody))
            {
                return cachedResponseBody;
            }

            var request = new HttpRequestMessage(method, requestUrl);

            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", azToken);

            HttpResponseMessage response = await httpClient.SendAsync(request);

            response.EnsureSuccessStatusCode();
            string responseBody = await response.Content.ReadAsStringAsync();

            _cache.Set(requestUrl, responseBody);

            return responseBody;
        }

        public Template GenerateEmptyTemplate()
        {
            TemplateCreator templateCreator = new TemplateCreator();
            Template armTemplate = templateCreator.CreateEmptyTemplate();
            return armTemplate;
        }

        public Template GenerateEmptyPropertyTemplateWithParameters()
        {
            Template armTemplate = GenerateEmptyTemplate();
            armTemplate.parameters = new Dictionary<string, TemplateParameterProperties> { { ParameterNames.ApimServiceName, new TemplateParameterProperties() { type = "string" } } };
            return armTemplate;
        }

        protected async Task<string> GetNextPageAsync(string nextLink)
        {
            (string azToken, string _) = await auth.GetAccessToken();
            return await CallApiManagementAsync(azToken, nextLink);
        }

        protected async Task ForEachEntity(string response, Func<JToken, Task> onItem)
        {
            string nextLink = null;

            do
            {
                var result = JObject.Parse(response);
                nextLink = result.ContainsKey("nextLink") ? ((JValue)result["nextLink"]).Value<string>() : null;

                foreach (var item in (JArray)(result["value"]))
                    await onItem.Invoke(item);

                if (nextLink != null)
                    response = await GetNextPageAsync(nextLink);
            }
            while (nextLink != null);
        }
    }
}
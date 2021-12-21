using System;
using System.Collections.Generic;
using System.Linq;

namespace apimtemplate.Yaml
{
    public static class Helpers
    {

        public static string GetProductName(string productName)
        {
            return GetApiName(productName);
        }
        public static string GetApiName(string sourceApiName)
        {
            var index = sourceApiName.IndexOf("-");
            if (index != -1)
            {
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
    }
}

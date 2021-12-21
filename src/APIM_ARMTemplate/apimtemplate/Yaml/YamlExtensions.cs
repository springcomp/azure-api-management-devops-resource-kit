using System.Collections.Generic;

namespace apimtemplate.Yaml
{
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

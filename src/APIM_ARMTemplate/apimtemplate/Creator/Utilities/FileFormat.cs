using Newtonsoft.Json;
using System;

namespace apimtemplate.Creator.Utilities
{
    public static class FileFormat
    {
        public static bool IsUri(string openApiSpec, out Uri uriResult)
        {
            return
                Uri.TryCreate(openApiSpec, UriKind.Absolute, out uriResult) &&
                (uriResult.Scheme == Uri.UriSchemeHttp || uriResult.Scheme == Uri.UriSchemeHttps)
                ;
        }

        public static bool IsJson(string fileContents)
        {
            try
            {
                object deserializedFileContents = JsonConvert.DeserializeObject<object>(fileContents);
                return true;
            }
            catch (Exception)
            {
                return false;
            }
        }
    }
}

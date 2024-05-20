
namespace Microsoft.Azure.Management.ApiManagement.ArmTemplates.Common
{
    public class PolicyFragmentTemplateResource : APITemplateSubResource
    {
        public PolicyFragmentProperties properties { get; set; }
    }

    public class PolicyFragmentProperties
    {
        public string description {get; set; }
        public string value { get; set; }
    }
}

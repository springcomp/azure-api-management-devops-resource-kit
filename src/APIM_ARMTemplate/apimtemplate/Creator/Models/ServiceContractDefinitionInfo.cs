namespace Microsoft.Azure.Management.ApiManagement.ArmTemplates.Create
{
    public sealed class ServiceContractDefinitionInfo
    {
        // one of swagger[-link]-json or openapi[-link|+json]
        public string Format { get; set; }
        // either an uri or a path to a file
        public string Value { get; set; }
    }
}
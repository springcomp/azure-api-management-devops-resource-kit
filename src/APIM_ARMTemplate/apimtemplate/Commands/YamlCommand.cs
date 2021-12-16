using apimtemplate.Yaml;
using McMaster.Extensions.CommandLineUtils;
using System;
using System.ComponentModel;
using System.Linq;

namespace Microsoft.Azure.Management.ApiManagement.ArmTemplates.Create
{
    public class YamlCommand : CommandLineApplication
    {
        public YamlCommand()
        {
            this.Name = "yaml";
            this.Description = "Convert Template to Yaml";
            AddExtractorConfigPropertiesToCommandLineOptions();


            this.OnExecute(async () => {

                var config = new YamlExtractorConfig();

                UpdateExtractorConfigFromAdditionalArguments(config);

                config.Validate();

                await YamlExtractorUtils.ExtractAsync(config);

                Console.WriteLine("Creating global service policy template");
                Console.WriteLine("------------------------------------------");

                
            });

        }

        private void AddExtractorConfigPropertiesToCommandLineOptions()
        {
            foreach (var propertyInfo in typeof(YamlExtractorConfig).GetProperties())
            {
                var description = Attribute.IsDefined(propertyInfo, typeof(DescriptionAttribute)) ? (Attribute.GetCustomAttribute(propertyInfo, typeof(DescriptionAttribute)) as DescriptionAttribute).Description : string.Empty;

                this.Option($"--{propertyInfo.Name} <{propertyInfo.Name}>", description, CommandOptionType.SingleValue);
            }
        }
        private void UpdateExtractorConfigFromAdditionalArguments(YamlExtractorConfig extractorConfig)
        {
            var extractorConfigType = typeof(YamlExtractorConfig);
            foreach (var option in this.Options.Where(o => o.HasValue()))
            {
                extractorConfigType.GetProperty(option.LongName)?.SetValue(extractorConfig, option.Value());
            }
        }
    }
}

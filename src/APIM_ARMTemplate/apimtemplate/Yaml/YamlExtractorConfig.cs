using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Text;

namespace apimtemplate.Yaml
{
    public class YamlExtractorConfig
    {

        [Description("Name of the extracted API Management name")]
        public string apimName { get; set; }


        [Description("Path of the ARM Files")]
        public string initPath { get; set; }

        [Description("Product Keys Path - default is initPath")]
        public string productKeyPath { get; set; }

        [Description("Named Values Path - default is initPath")]
        public string namedValuesPath { get; set; }


        public void Validate()
        {
            if (String.IsNullOrEmpty(apimName))
                throw new ArgumentNullException("Missing <apimName>");

            if (String.IsNullOrEmpty(initPath))
                throw new ArgumentNullException("Missing <initPath>");

        }

    }
}

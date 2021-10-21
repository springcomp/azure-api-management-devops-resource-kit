using System;
using Xunit;
using apimtemplate.Creator.Utilities;

namespace apimtemplate.test.Creator.UtilitiesTests
{
    public sealed class FileFormatTests
    {
        [Fact]
        public void FileFormat_IsUri()
        {
            Assert.False(FileFormat.IsUri(@"c:\path\to\swagger.json", out Uri _));
            Assert.True(FileFormat.IsUri(@"https://host.example.com/v2/api_docs/", out Uri _));
        }

        [Fact]
        public void FileFormat_IsJson()
        {
            Assert.False(FileFormat.IsJson("document:\r\n  yaml: true"));
            Assert.True(FileFormat.IsJson("{}"));
        }
    }
}

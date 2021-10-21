using apimtemplate.Creator.Utilities;
using apimtemplate.Creator.Utilities.Internals;
using System.Collections.Generic;
using System.Threading.Tasks;
using Xunit;

namespace apimtemplate.test.Creator.UtilitiesTests
{
    public sealed class UriOrPathTests
    {
        [Fact]
        public async Task UriOrPath_RetrieveLocalFileContents()
        {
            var fsShim = new FilesystemShim( new Dictionary<string, string> { [@"c:\temp\file.txt"] = "Hello, world!", });

            // system under test

            var uop = new UriOrPath(@"c:\temp\file.txt", fsShim);

            // assert expectations

            Assert.Equal("Hello, world!", await uop.RetrieveContentAsync());
            Assert.Equal("Hello, world!", await uop.RetrieveContentAsync());
            Assert.Equal(1, fsShim.GetRetrieveCount(uop));
        }
    }
}

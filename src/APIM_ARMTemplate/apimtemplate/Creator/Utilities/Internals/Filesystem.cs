using System.IO;
using apimtemplate.Creator.Interop;

namespace apimtemplate.Creator.Utilities.Internals
{
    internal sealed class Filesystem : IFilesystem
    {
        public string ReadAllText(string path)
        {
            return File.ReadAllText(path);
        }
    }
}
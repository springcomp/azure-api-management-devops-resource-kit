using System.Collections.Generic;
using System.IO;
using apimtemplate.Creator.Interop;

namespace apimtemplate.Creator.Utilities.Internals
{
    internal class FilesystemShim : IFilesystem
    {
        private readonly IDictionary<string, string> collection_;
        private readonly IDictionary<string, int> counts_
            = new Dictionary<string, int>();

        public FilesystemShim(IDictionary<string, string> dictionary)
        {
            collection_ = dictionary;
        }

        public int GetRetrieveCount(string path)
            => counts_[path];

        public string ReadAllText(string path)
        {
            if (collection_.ContainsKey(path))
            {
                if (!counts_.ContainsKey(path))
                    counts_[path] = 0;
                ++counts_[path];

                return collection_[path];
            }

            throw new FileNotFoundException();
        }
    }
}
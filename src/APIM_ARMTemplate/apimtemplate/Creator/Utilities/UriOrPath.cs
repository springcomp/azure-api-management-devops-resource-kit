using apimtemplate.Creator.Interop;
using apimtemplate.Creator.Utilities.Internals;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace apimtemplate.Creator.Utilities
{
    public sealed class UriOrPath : IContentRetriever
    {
        private readonly IFilesystem filesystem_;

        private readonly IDictionary<string, string> contents_
            = new Dictionary<string, string>();


        private readonly string uriOrPath_;
        private readonly bool isUri_;

        internal UriOrPath(string uriOrPath, IFilesystem fs)
        {
            filesystem_ = fs;

            uriOrPath_ = uriOrPath;
            isUri_ = FileFormat.IsUri(uriOrPath);
        }

        public UriOrPath(string uriOrPath)
            : this (uriOrPath, new Filesystem())
        {
        }

        public static implicit operator string(UriOrPath uop)
            => uop.uriOrPath_;

        public bool IsUri
            => isUri_;

        public Task<string> RetrieveContentAsync()
        {
            if (contents_.ContainsKey(uriOrPath_))
                return Task.FromResult(contents_[uriOrPath_]);

            if (isUri_)
                throw new NotSupportedException();

            var contents = filesystem_.ReadAllText(uriOrPath_);

            contents_[uriOrPath_] = contents;

            return Task.FromResult(contents);
        }
    }
}

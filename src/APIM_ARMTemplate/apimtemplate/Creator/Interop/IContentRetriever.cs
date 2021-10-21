using System.Threading.Tasks;

namespace apimtemplate.Creator.Interop
{
    /// <summary>
    ///  Represents an in-memory cache of a content (path or uri)
    ///  that can be retrieved multiple times.
    /// </summary>
    public interface IContentRetriever
    {
        Task<string> RetrieveContentAsync();
    }
}
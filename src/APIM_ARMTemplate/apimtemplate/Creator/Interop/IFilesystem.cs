namespace apimtemplate.Creator.Interop
{
    internal interface IFilesystem
    {
        public string ReadAllText(string path);
    }
}
namespace GMServerWebPanel.API.Shared
{
    public interface IFileSystemService
    {
        public void WriteTextFile(string fileName, string text);
    }
}

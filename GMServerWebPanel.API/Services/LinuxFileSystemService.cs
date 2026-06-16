using GMServerWebPanel.API.Services.Interfaces;

namespace GMServerWebPanel.API.Services
{
    public class LinuxFileSystemService : IFileSystemService
    {
        public void WriteTextFile(string fileName, string text)
        {
            File.WriteAllText(fileName, text);
        }
    }
}

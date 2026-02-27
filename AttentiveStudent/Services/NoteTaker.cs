using System.Text;

namespace CaptureService.Services
{
    public class NoteTaker : IDisposable
    {
        string pathToNote;
        bool deleteAfter = true;
        public NoteTaker(string pathToExeFolder)
        {
            string outputFolder = Path.Combine(pathToExeFolder, "Notes");
            Directory.CreateDirectory(outputFolder);
            string fileName = DateTime.Now.ToString().Replace('.', '_').Replace(':', '_').Replace(' ', '_');
            pathToNote = Path.Combine(outputFolder, $"{fileName}.txt");
        }
        public void TakeNote(string text)
        {
            if (deleteAfter)
            {
                deleteAfter = false;
            }
            using (var fileStream = File.Open(pathToNote, FileMode.Append, FileAccess.Write))
            {
                using (var writer = new StreamWriter(fileStream, Encoding.UTF8))
                {
                    writer.Write($"{text} ");
                }
            }
        }
        public void Dispose()
        {
            if (deleteAfter)
            {
                File.Delete(pathToNote);
            }
        }
    }
}

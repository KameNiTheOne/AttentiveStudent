using ConfigChangeReactor;
using System.Text;

namespace CaptureService.Services
{
    public class NoteTaker : Configurable
    {
        string pathToNote;
        string pathToBatch;

        int useNotedAmount;
        public NoteTaker(Dictionary<string, string> cnfg)
        {
            string outputFolder = Path.Combine(cnfg["pathToExeFolder"], "Notes");
            Directory.CreateDirectory(outputFolder);
            string fileName = DateTime.Now.ToString().Replace('.', '_').Replace(':', '_').Replace(' ', '_');
            pathToNote = Path.Combine(outputFolder, $"{fileName}.txt");
            pathToBatch = Path.Combine(outputFolder, $"Batch.txt");

            useNotedAmount = int.Parse(cnfg["useNotedAmount"]);

            ReactorDomain.Subscribe(ChangeHandler);
        }
        public void TakeNote(string text, bool takeNote)
        {
            using (var fileStream = File.Open(pathToBatch, FileMode.Append, FileAccess.Write))
            {
                using (var writer = new StreamWriter(fileStream, Encoding.UTF8))
                {
                    writer.Write($"{text} ");
                }
            }
            if (takeNote)
            {
                using (var fileStream = File.Open(pathToNote, FileMode.Append, FileAccess.Write))
                {
                    using (var writer = new StreamWriter(fileStream, Encoding.UTF8))
                    {
                        writer.Write($"{text} ");
                    }
                }
            }
        }
        public string ReadFromBatch()
        {
            using (var fileStream = File.Open(pathToBatch, FileMode.Open, FileAccess.Read))
            {
                using (var reader = new StreamReader(fileStream, Encoding.UTF8))
                {
                    string read = reader.ReadToEnd();
                    IEnumerable<char> reversed = read.Reverse();

                    int wordCount = useNotedAmount;
                    int wordspos = 0;
                    char marker = ' ';
                    foreach (char ch in reversed)
                    {
                        if (wordCount < 0)
                        {
                            break;
                        }
                        if (ch == marker)
                        {
                            wordCount--;
                        }
                        wordspos++;
                    }
                    string result = read.Substring(read.Length - wordspos);
                    return result;
                }
            }
        }
        public override void ChangeHandler(Dictionary<string, string> cnfg)
        {
            useNotedAmount = int.Parse(cnfg["useNotedAmount"]);
        }

        public override Task Dispose()
        {
            File.Delete(pathToBatch);
            return base.Dispose();
        }
    }
}

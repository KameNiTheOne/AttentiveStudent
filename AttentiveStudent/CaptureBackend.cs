using CaptureService.Services;

namespace CaptureService
{
    public class CaptureBackend : IDisposable
    {
        int audiofilesAmount;
        string outputFolderPath;
        AudioCapture audioCapture;

        Func<string, Task> transcriptionAction;

        public bool TakeNotes = false;

        public CaptureBackend(Dictionary<string, string> cnfg, Func<string, Task> _transcriptionAction)
        {
            audiofilesAmount = int.Parse(cnfg["audiofilesAmount"]);

            transcriptionAction = _transcriptionAction;

            Console.WriteLine("Initializing AudioCapture...");
            string ptef = cnfg["pathToExeFolder"];
            outputFolderPath = Path.Combine(ptef, "Recorded");
            audioCapture = new AudioCapture(outputFolderPath, cnfg["captureDeviceName"], long.Parse(cnfg["audiofileDuration"]));
            Console.WriteLine("Done initializing AudioCapture!");
        }
        public async Task<(int, int)> StartListening(CancellationToken ct)
        {
            while (true)
            {
                for (int i = 0; i < audiofilesAmount; i++)
                {
                    string outputTempPath = GetAudiofilePath(i, true);
                    int duration = await audioCapture.Capture(outputTempPath, ct);
                    await handleDeletingTemp(outputTempPath, GetAudiofilePath(i));

                    if (ct.IsCancellationRequested)
                    {
                        return (i, duration);
                    }
                }
            }
        }
        async Task handleDeletingTemp(string outputTempPath, string outputMainPath)
        {
            if (File.Exists(outputMainPath))
            {
                if (TakeNotes)
                {
                    await transcriptionAction(outputMainPath);
                }
                File.Delete(outputMainPath);
            }
            File.Move(outputTempPath, outputMainPath);
        }
        string GetAudiofilePath(int numberORecording, bool temp = false)
        {
            string suffix = temp ? "temp" : "";
            return Path.Combine(outputFolderPath, $"recorded{numberORecording}{suffix}.wav");
        }
        public string[] ArrangeForTranscription(int transcriptionStartPos)
        {
            string[] result = new string[audiofilesAmount];
            int pos = transcriptionStartPos + 1;
            for (int i = 0; i < audiofilesAmount; ++i)
            {
                if (pos == audiofilesAmount)
                {
                    pos = 0;
                }
                result[pos] = GetAudiofilePath(pos, false);
                pos++;
            }
            return result;
        }
        public void Dispose()
        {
            audioCapture.Dispose();
        }
    }
}
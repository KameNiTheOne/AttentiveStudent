using CaptureService.Services;

namespace CaptureService
{
    public class CaptureBackend : IDisposable
    {
        int audiofilesAmount;
        string outputFolderPath;
        AudioCapture audioCapture;
        Transcriber transcriber;
        public NoteTaker noteTaker;

        Random rnd = new Random();

        Task asyncTranscription = Task.CompletedTask;
        Task<(int, int)> asyncListening;

        Dictionary<int, string> capturedPaths = new();
        CancellationTokenSource captureCts = new();
        bool captureCtsDisposed = false;

        public bool TakeNotes = false;
        private CaptureBackend() { }
        public static async Task<CaptureBackend> Initialize(Dictionary<string, string> cnfg)
        {
            var instance = new CaptureBackend();
            instance.audiofilesAmount = int.Parse(cnfg["audiofilesAmount"]);

            Console.WriteLine("Initializing the NoteTaker...");
            instance.noteTaker = new NoteTaker(cnfg["pathToExeFolder"]);
            Console.WriteLine($"Done initializing the NoteTaker!");

            Console.WriteLine("Initializing the transcriber...");
            instance.transcriber = await Transcriber.Initialize(cnfg);
            Console.WriteLine($"Done initializing the transcriber!");

            Console.WriteLine("Initializing AudioCapture...");
            string ptef = cnfg["pathToExeFolder"];
            instance.outputFolderPath = Path.Combine(ptef, "Recorded");
            instance.audioCapture = new AudioCapture(instance.outputFolderPath, cnfg["captureDeviceName"], long.Parse(cnfg["audiofileDuration"]));
            Console.WriteLine("Done initializing AudioCapture!");

            instance.asyncListening = instance.startListening(instance.captureCts.Token);

            return instance;
        }
        async Task<(int, int)> startListening(CancellationToken ct)
        {
            while (true)
            {
                for (int i = 0; i < audiofilesAmount; i++)
                {
                    string outputTempPath = getAudiofilePath(i, true);
                    capturedPaths[i] = outputTempPath;

                    int duration = await audioCapture.Capture(outputTempPath, ct);
                    await handleDeletingTemp(outputTempPath, getAudiofilePath(i));

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
                    string noteTempPath = Path.Combine(outputFolderPath, $"temp{rnd.Next(1_000_000)}.wav");
                    File.Copy(outputMainPath, noteTempPath);

                    await noteTranscriptionAction(noteTempPath);
                }
                File.Delete(outputMainPath);
            }
            File.Move(outputTempPath, outputMainPath);
        }
        async Task noteTranscriptionAction(string outputMainPath)
        {
            Func<string, Task> Transcribe = async (string AFPath) =>
            {
                string transcribed = await transcriber.Transcribe(AFPath, CancellationToken.None);
                noteTaker.TakeNote(transcribed);
            };

            await asyncTranscription;
            asyncTranscription = Transcribe(outputMainPath);
        }
        public async Task<string> TranscribeMultiple(bool continueCapture, CancellationToken ct)
        {
            (int, int) transcriptionInfo = await stopListeningAndReturnTranscriptionInfo(continueCapture);
            int startPos = transcriptionInfo.Item1;
            int lastFileDuration = transcriptionInfo.Item2;
            string[] arrangedAudiofiles = arrangeForTranscription(startPos);

            await asyncTranscription;
            asyncTranscription = transcriber.TranscribeMultiple(arrangedAudiofiles, lastFileDuration, ct);

            return await (Task<string>) asyncTranscription;
        }
        async Task<(int, int)> stopListeningAndReturnTranscriptionInfo(bool continueCapture)
        {
            if (TakeNotes) await asyncTranscription;
            captureCts.Cancel();
            (int, int) info = await asyncListening;
            captureCts.Dispose();
            captureCtsDisposed = true;

            if (continueCapture)
            {
                captureCtsDisposed = false;
                captureCts = new CancellationTokenSource();
                asyncListening = startListening(captureCts.Token);
            }
            return info;
        }
        string[] arrangeForTranscription(int transcriptionStartPos)
        {
            int amount = capturedPaths.Count;
            string[] result = new string[amount];
            int pos = transcriptionStartPos + 1;
            for (int i = 0; i < amount; ++i)
            {
                if (pos == audiofilesAmount)
                {
                    pos = 0;
                }
                result[pos] = getAudiofilePath(pos, false);
                pos++;
            }
            return result;
        }
        string getAudiofilePath(int numberORecording, bool temp = false)
        {
            string suffix = temp ? "temp" : "";
            return Path.Combine(outputFolderPath, $"recorded{numberORecording}{suffix}.wav");
        }
        public void Dispose()
        {
            if (!captureCtsDisposed) captureCts.Cancel();
            Task.WaitAll(asyncTranscription, asyncListening);
            if (!captureCtsDisposed) captureCts.Dispose();
            audioCapture.Dispose();
            noteTaker.Dispose();
        }
    }
}
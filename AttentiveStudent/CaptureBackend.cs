using CaptureService.Services;
using ConfigChangeReactor;

namespace CaptureService
{
    public class CaptureBackend : Configurable
    {
        string outputFolderPath;
        AudioCapture audioCapture;
        Transcriber transcriber;
        public NoteTaker noteTaker;

        Random rnd = new Random();

        Task asyncTranscription;
        Task asyncListening;

        CancellationTokenSource captureCts = new();
        bool captureCtsDisposed = false;

        public bool TakeNotes = false;
        private CaptureBackend() { }
        public static async Task<CaptureBackend> Initialize(Dictionary<string, string> cnfg)
        {
            var instance = new CaptureBackend();

            Console.WriteLine("Initializing the NoteTaker...");
            instance.noteTaker = new NoteTaker(cnfg);
            Console.WriteLine($"Done initializing the NoteTaker!");

            Console.WriteLine("Initializing the transcriber...");
            instance.transcriber = await Transcriber.Initialize(cnfg);
            Console.WriteLine($"Done initializing the transcriber!");

            Console.WriteLine("Initializing AudioCapture...");
            instance.outputFolderPath = Path.Combine(cnfg["pathToExeFolder"], "Recorded");
            Directory.CreateDirectory(instance.outputFolderPath);
            instance.audioCapture = new AudioCapture(cnfg);
            Console.WriteLine("Done initializing AudioCapture!");

            Func<Task<string>> completedStringTask = async () => { await Task.CompletedTask; return ""; };
            instance.asyncTranscription = completedStringTask();

            instance.asyncListening = instance.StartListeningAsync(instance.captureCts.Token);

            ReactorDomain.Subscribe(instance.ChangeHandler);
            return instance;
        }
        async Task StartListeningAsync(CancellationToken ct)
        {
            while (true)
            {
                string audiofilePath = Path.Combine(outputFolderPath, $"{rnd.Next(1_000_000)}.wav");
                int duration = await audioCapture.Capture(audiofilePath, ct);
                await HandleDeletingTemp(audiofilePath, duration);

                if (ct.IsCancellationRequested)
                {
                    break;
                }
            }
        }
        async Task HandleDeletingTemp(string outputMainPath, int duration)
        {
            if (File.Exists(outputMainPath))
            {
                string noteTempPath = Path.Combine(outputFolderPath, $"temp{rnd.Next(1_000_000)}.wav");
                File.Copy(outputMainPath, noteTempPath);

                await NoteTranscriptionAction(noteTempPath, duration);

                File.Delete(outputMainPath);
            }
        }
        async Task NoteTranscriptionAction(string outputMainPath, int duration)
        {
            Func<string, int, Task> Transcribe = async (string AFPath, int duration) =>
            {
                string transcribed = await transcriber.Transcribe(AFPath, duration, CancellationToken.None);
                noteTaker.TakeNote(transcribed, TakeNotes);
            };

            await asyncTranscription;
            asyncTranscription = Transcribe(outputMainPath, duration);
        }
        public async Task<string> StopAndTranscribe(bool continueCapture, CancellationToken ct)
        {
            await StopListeningAndReturnTranscriptionDuration(continueCapture);
            await asyncTranscription;

            return noteTaker.ReadFromBatch();
        }
        async Task StopListeningAndReturnTranscriptionDuration(bool continueCapture)
        {
            if (TakeNotes) await asyncTranscription;
            captureCts.Cancel();
            await asyncListening;
            captureCts.Dispose();
            captureCtsDisposed = true;

            if (continueCapture)
            {
                captureCtsDisposed = false;
                captureCts = new CancellationTokenSource();
                asyncListening = StartListeningAsync(captureCts.Token);
            }
        }
        public override void ChangeHandler(Dictionary<string, string> cnfg)
        {
            StopListeningAndReturnTranscriptionDuration(true).Wait();
        }

        public override async Task Dispose()
        {
            if (!captureCtsDisposed) captureCts.Cancel();
            await asyncListening;
            await asyncTranscription;
            if (!captureCtsDisposed) captureCts.Dispose();

            Directory.Delete(outputFolderPath, true);
            await audioCapture.Dispose();
            await noteTaker.Dispose();
            await base.Dispose();
        }
    }
}
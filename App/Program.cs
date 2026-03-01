using CaptureService;
using CaptureService.Services;
using GPTService;
using AttentiveStudent;
class Program
{
    static CancellationTokenSource exitCts;
    static CancellationTokenSource captureCts;

    static Localization localization;
    static Transcriber transcriber;
    static CaptureBackend captureBackend;
    static NoteTaker noteTaker;
    static IGPT gpt;

    static string toDisplay = "";
    public static Task[] AsyncDomain = new Task[2];

    static async Task Main()
    {
        //Initialze config
        string pathToExeFolder = AppDomain.CurrentDomain.BaseDirectory;

        string pathToConfigFolder = Path.Combine(pathToExeFolder, "Settings");
        string pathToConfig = Path.Combine(pathToConfigFolder, "appsettings.json");

        localization = new Localization(pathToExeFolder);
        Dictionary<string, string> config;
        try
        {
            config = Instruments.DeserializeObjectFromFile<Dictionary<string, string>>(pathToConfig);
        }
        catch
        {
            Console.WriteLine(localization.Items["configError"]);
            Console.ReadLine();
            return;
        }
        if (!Directory.Exists(Path.Combine(pathToExeFolder, "Transcriber", "whisper-small")))
        config["pathToExeFolder"] = pathToExeFolder;

        exitCts = new CancellationTokenSource();
        captureCts = new CancellationTokenSource();

        Console.WriteLine("Initializing the transcriber...");
        transcriber = await Transcriber.Initialize(config);
        Console.WriteLine($"Done initializing the transcriber!");

        Console.WriteLine("Initializing the GPT...");
        GPTBuilder gptBuilder = new(config, localization.Items);
        gpt = gptBuilder.Build();
        Console.WriteLine($"Done initializing the GPT!");

        Console.WriteLine("Initializing the NoteTaker...");
        noteTaker = new NoteTaker(pathToExeFolder);
        Console.WriteLine($"Done initializing the NoteTaker!");

        Console.WriteLine("Initializing the Capture...");
        captureBackend = new CaptureBackend(config, noteTranscriptionAction);
        AsyncDomain[0] = captureBackend.StartListening(captureCts.Token);
        AsyncDomain[1] = Task.Delay(1);
        Console.WriteLine($"Done initializing the Capture!");

        await MenuLoop();
    }
    static async Task MenuLoop()
    {
        bool exit = false;

        ChangeAndWriteLineDisplayed($"{localization.Items["greetingText"]} {localization.Items["mainMenu"]}");
        while (!exit)
        {
            string inputedKey = Console.ReadKey().Key.ToString();
            switch (inputedKey)
            {
                case "D1": //Transcribe and send to GPT
                    string result = await TranscribeAllAndMaybeSendToGPT();
                    ChangeAndWriteLineDisplayed($"{localization.Items["gptResponseText"]}" +
                        $"\n\n{result}\n\n" +
                        $"{localization.Items["mainMenu"]}");
                    break;
                case "D2": //Enable/Disable note taking mode
                    captureBackend.TakeNotes = !captureBackend.TakeNotes;
                    ChangeAndWriteLineDisplayed($"{localization.Items["takeNotesToggleText"]} " +
                        $"{captureBackend.TakeNotes}.\n\n" +
                        $"{localization.Items["mainMenu"]}");
                    break;
                case "D3": //Help
                    ChangeAndWriteLineDisplayed($"{localization.Items["helpText"]}\n\n" +
                        $"{localization.Items["mainMenu"]}");
                    break;
                case "D4": //Exit the program
                    ChangeAndWriteLineDisplayed($"{localization.Items["exitText"]}\n");
                    localization.Items["transcriptMenu"] = $"{localization.Items["exitText"]}\n\n{localization.Items["transcriptMenu"]}";
                    await ExitProgram();
                    exit = true;
                    break;
                default:
                    WriteLineDisplayed();
                    break;
            }
            await Task.Delay(20);
        }
    }
    static async Task NotifyWhenTaskDone(Task task)
    {
        while (!task.IsCompleted)
        {
            await Task.Delay(200);
        }
        if (task.IsCompletedSuccessfully)
        {
            toDisplay += $"\n\n{localization.Items["notificationText"]}";
            WriteLineDisplayed();
        }
    }
    static async Task noteTranscriptionAction(string outputMainPath)
    {
        Func<string, Task> Transcribe = async (string AFPath) =>
        {
            string transcribed = await transcriber.Transcribe(AFPath, CancellationToken.None);
            noteTaker.TakeNote(transcribed);
        };

        await AsyncDomain[1];
        AsyncDomain[1] = Transcribe(outputMainPath);
    }
    static async Task<string> TranscribeAllAndMaybeSendToGPT(bool sendToGPT = true, bool continueCapture = true)
    {
        CancellationTokenSource interruptCts = new();

        (int,int) transcriptionInfo = await StopListeningAndReturnTranscriptionInfo(continueCapture);
        int startPos = transcriptionInfo.Item1;
        int lastFileDuration = transcriptionInfo.Item2;
        string[] arrangedAudiofiles = captureBackend.ArrangeForTranscription(startPos);
        await AsyncDomain[1];
        AsyncDomain[1] = transcriber.TranscribeMultiple(arrangedAudiofiles, lastFileDuration, interruptCts.Token);

        Task notify = NotifyWhenTaskDone(AsyncDomain[1]);

        //UI loop for cancelling transcription
        ChangeAndWriteLineDisplayed($"{localization.Items["transcriptMenu"]}");
        while (!(interruptCts.IsCancellationRequested || AsyncDomain[1].IsCompletedSuccessfully))
        {
            string inputedKey = Console.ReadKey().Key.ToString();
            switch (inputedKey)
            {
                case "C": //Cancel
                    interruptCts.Cancel();
                    break;
                default:
                    WriteLineDisplayed();
                    break;
            }
            await Task.Delay(20);
        }
        await notify;

        string transcribed = await (Task<string>)AsyncDomain[1];

        if (sendToGPT)
        {
            string query = $"{localization.Items["gptPrompt"]} {transcribed}";
            Task<string> gptTask = gpt.Query(query, interruptCts.Token);

            notify = NotifyWhenTaskDone(gptTask);

            //UI loop for cancelling gpt query
            ChangeAndWriteLineDisplayed($"{localization.Items["gptMenu"]}");
            while (!(interruptCts.IsCancellationRequested || gptTask.IsCompletedSuccessfully))
            {
                string inputedKey = Console.ReadKey().Key.ToString();
                switch (inputedKey)
                {
                    case "C": //Cancel
                        interruptCts.Cancel();
                        break;
                    default:
                        WriteLineDisplayed();
                        break;
                }
                await Task.Delay(20);
            }
            await notify;

            if (!gptTask.IsCompletedSuccessfully)
            {
                return "";
            }
            return gptTask.Result;
        }
        return transcribed;
    }
    public static async Task<(int, int)> StopListeningAndReturnTranscriptionInfo(bool continueCapture)
    {
        if (captureBackend.TakeNotes) await AsyncDomain[1];
        captureCts.Cancel();
        (int, int) info = await (Task<(int, int)>)AsyncDomain[0];
        captureCts.Dispose();

        if (continueCapture)
        {
            captureCts = new CancellationTokenSource();
            AsyncDomain[0] = captureBackend.StartListening(captureCts.Token);
        }
        return info;
    }
    static async Task ExitProgram()
    {
        if (captureBackend.TakeNotes)
        {
            string transcribed = await TranscribeAllAndMaybeSendToGPT(false, false);
            noteTaker.TakeNote(transcribed);
        }

        if (!captureBackend.TakeNotes) captureCts.Cancel();
        exitCts.Cancel();
        await Task.WhenAll(AsyncDomain);
        if (!captureBackend.TakeNotes) captureCts.Dispose();
        exitCts.Dispose();

        captureBackend.Dispose();
        noteTaker.Dispose();

        toDisplay += $"\n\n{localization.Items["notificationText"]}";
        WriteLineDisplayed();
        Console.ReadLine();
    }
    static void ChangeAndWriteLineDisplayed(string str)
    {
        toDisplay = str;
        WriteLineDisplayed();
    }
    static void WriteLineDisplayed()
    {
        Console.Clear();
        Console.WriteLine(toDisplay);
    }
}

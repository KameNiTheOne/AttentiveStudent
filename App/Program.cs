using CaptureService;
using GPTService;
using AttentiveStudent;
class Program
{
    static Localization localization;
    static CaptureBackend captureBackend;
    static IGPT gpt;

    static string toDisplay = "";

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

        Console.WriteLine("Initializing the GPT...");
        GPTBuilder gptBuilder = new(config, localization.Items);
        gpt = gptBuilder.Build();
        Console.WriteLine($"Done initializing the GPT!");

        Console.WriteLine("Initializing the Capture...");
        captureBackend = await CaptureBackend.Initialize(config);
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
                    string result = await TranscribeAllAndSendToGPT();
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
            AddToAndWriteLineDisplayed($"\n\n{localization.Items["notificationText"]}");
        }
    }
    static async Task<string> TranscribeAllAndSendToGPT()
    {
        CancellationTokenSource interruptCts = new();

        Task<string> transcriptionTask = captureBackend.TranscribeMultiple(true, interruptCts.Token);

        Task notify = NotifyWhenTaskDone(transcriptionTask);

        //UI loop for cancelling transcription
        ChangeAndWriteLineDisplayed($"{localization.Items["transcriptMenu"]}");
        while (!(interruptCts.IsCancellationRequested || transcriptionTask.IsCompletedSuccessfully))
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

        string transcribed = await transcriptionTask;

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
    static async Task ExitProgram()
    {
        if (captureBackend.TakeNotes)
        {
            string transcribed = await captureBackend.TranscribeMultiple(false, CancellationToken.None);
            captureBackend.noteTaker.TakeNote(transcribed);
        }

        captureBackend.Dispose();

        AddToAndWriteLineDisplayed(localization.Items["notificationText"]);
        Console.ReadLine();
    }
    static void AddToAndWriteLineDisplayed(string str)
    {
        toDisplay += str;
        WriteLineDisplayed();
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

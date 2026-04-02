using ConfigDomain;
using CaptureService;
using GPTService;
class Program
{
    static Localization localization;
    static CaptureBackend captureBackend;
    static IGPT gpt;

    static string toDisplay = "";

    static async Task Main()
    {
        Dictionary<string, string> config = Config.Initialize();
        localization = Config.localization;

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
                    string result = await TranscribeAndSendToGPT();
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

                case "D4": //Run config initialization and then exit the program
                    Config.Change(out bool restartRequired);
                    if (restartRequired)
                    {
                        ChangeAndWriteLineDisplayed($"{localization.Items["exitText"]}\n");
                        localization.Items["transcriptMenu"] = $"{localization.Items["exitText"]}\n\n{localization.Items["transcriptMenu"]}";
                        await ExitProgram();
                        exit = true;
                    }
                    else
                    {
                        WriteLineDisplayed();
                    }
                    break;

                case "D5": //Exit the program
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
    static async Task<string> TranscribeAndSendToGPT()
    {
        Task<string> transcriptionTask = captureBackend.StopAndTranscribe(true, CancellationToken.None);
        Task notify = NotifyWhenTaskDone(transcriptionTask);

        ChangeAndWriteLineDisplayed($"{localization.Items["transcriptMenu"]}");
        await notify;
        string transcribed = await transcriptionTask;

        string query = $"{localization.Items["gptPrompt"]} {transcribed}";
        Task<string> gptTask = gpt.Query(query, CancellationToken.None);
        notify = NotifyWhenTaskDone(gptTask);

        ChangeAndWriteLineDisplayed($"{localization.Items["gptMenu"]}");
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
            string transcribed = await captureBackend.StopAndTranscribe(false, CancellationToken.None);
        }

        await captureBackend.Dispose();

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

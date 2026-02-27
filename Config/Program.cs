using Misc;
using NAudio.CoreAudioApi;
using Newtonsoft.Json;
using System.Globalization;
using System.Text;

class Program
{
    static Localization localization;
    static string toDisplay = "";
    static void Main()
    {
        string pathToExeFolder = AppDomain.CurrentDomain.BaseDirectory;
        localization = new Localization(pathToExeFolder);

        string pathToConfigFolder = Path.Combine(pathToExeFolder, "Settings");
        string pathToConfig = Path.Combine(pathToConfigFolder, "appsettings.json");

        Dictionary<string, string> config;

        if (!File.Exists(pathToConfig))
        {
            string pathToTemplate = Path.Combine(pathToConfigFolder, "appsettingsTemplate.json");
            var templateConfig = Instruments.DeserializeObjectFromFile<Dictionary<string, string>>(pathToTemplate);
            config = InitialSetupMenu(templateConfig);
        }
        else
        {
            config = Instruments.DeserializeObjectFromFile<Dictionary<string, string>>(pathToConfig);
        }

        config = MainSetupMenu(config);

        string configjson = JsonConvert.SerializeObject(config);
        using (var fileStream = File.Open(pathToConfig, FileMode.Create, FileAccess.Write))
        {
            using (var writer = new StreamWriter(fileStream, Encoding.UTF8))
            {
                writer.Write(configjson);
            }
        }
    }
    static Dictionary<string, string> InitialSetupMenu(Dictionary<string, string> templateConfig)
    {
        Dictionary<string, string> result = templateConfig;

        ChangeGPTMode(ref result);
        if (result["gptMode"] == "local")
        {
            ChangeAndWriteLineDisplayed(localization.Items["contextSizeText"]);
            result["contextSize"] = ChooseInt().ToString();
            ChangeAndWriteLineDisplayed(localization.Items["gpuLayerCountText"]);
            result["gpuLayerCount"] = ChooseInt().ToString();
            ChangeAndWriteLineDisplayed(localization.Items["modelPathText"]);
            result["modelPath"] = Console.ReadLine();
        }
        else
        {
            ChangeAndWriteLineDisplayed(localization.Items["endpointText"]);
            result["endpoint"] = Console.ReadLine();
            ChangeAndWriteLineDisplayed(localization.Items["apiKeyText"]);
            result["apiKey"] = Console.ReadLine();
            ChangeAndWriteLineDisplayed(localization.Items["modelText"]);
            result["model"] = Console.ReadLine();
            ChangeAndWriteLineDisplayed(localization.Items["temperatureText"]);
            result["temperature"] = ChooseFloat().ToString();
        }
        ChangeCaptureDevice(ref result);

        return result;
    }
    static Dictionary<string, string> MainSetupMenu(Dictionary<string, string> configToChange)
    {
        Dictionary<string, string> result = configToChange;
        bool flag = true;
        while (flag)
        {
            List<string> options, keys;
            (options, keys) = MakeOptions(result);
            ChangeAndWriteLineDisplayed(localization.Items["mainSetupMenu"]);

            int chosen = ChooseFromList(options);
            flag = ChangersManager(keys[chosen], ref result);
        }
        return result;
    }
    static (List<string>, List<string>) MakeOptions(Dictionary<string, string> config)
    {
        List<string> result = new();
        List<string> keys = new();
        List<string> ignore;
        if (config["gptMode"] == "local")
        {
            ignore = new() { "endpoint", "apiKey", "model", "temperature" };
        }
        else
        {
            ignore = new() { "contextSize", "gpuLayerCount", "modelPath" };
        }
        foreach (string key in config.Keys)
        {
            if (ignore.Contains(key))
            {
                continue;
            }
            result.Add($"{key}\n   {localization.Items["currentText"]} {config[key]}\n");
            keys.Add(key);
        }
        result.Add(localization.Items["exitOption"]);
        keys.Add("EXIT");
        return (result, keys);
    }
    static bool ChangersManager(string fieldToChange, ref Dictionary<string, string> config)
    {
        switch (fieldToChange)
        {
            case "gptMode":
                {
                    ChangeGPTMode(ref config);
                    return true;
                }
            case "captureDeviceName":
                {
                    ChangeCaptureDevice(ref config);
                    return true;
                }
            case "audiofilesAmount":
                {
                    ChangeAndWriteLineDisplayed("");
                    config["audiofilesAmount"] = ChooseInt().ToString();
                    return true;
                }
            case "audiofileDuration":
                {
                    ChangeAndWriteLineDisplayed("");
                    config["audiofileDuration"] = ChooseInt().ToString();
                    return true;
                }
            case "endpoint":
                {
                    ChangeAndWriteLineDisplayed(localization.Items["endpointText"]);
                    config["endpoint"] = Console.ReadLine();
                    return true;
                }
            case "apiKey":
                {
                    ChangeAndWriteLineDisplayed(localization.Items["apiKeyText"]);
                    config["apiKey"] = Console.ReadLine();
                    return true;
                }
            case "model":
                {
                    ChangeAndWriteLineDisplayed(localization.Items["modelText"]);
                    config["model"] = Console.ReadLine();
                    return true;
                }
            case "temperature":
                {
                    ChangeAndWriteLineDisplayed(localization.Items["temperatureText"]);
                    config["temperature"] = ChooseFloat().ToString();
                    return true;
                }
            case "contextSize":
                {
                    ChangeAndWriteLineDisplayed(localization.Items["contextSizeText"]);
                    config["contextSize"] = ChooseInt().ToString();
                    return true;
                }
            case "gpuLayerCount":
                {
                    ChangeAndWriteLineDisplayed(localization.Items["gpuLayerCountText"]);
                    config["gpuLayerCount"] = ChooseInt().ToString();
                    return true;
                }
            case "modelPath":
                {
                    ChangeAndWriteLineDisplayed(localization.Items["modelPathText"]);
                    config["modelPath"] = Console.ReadLine();
                    return true;
                }
            default:
                {
                    return false;
                }
        }
    }
    static void ChangeGPTMode(ref Dictionary<string, string> config)
    {
        ChangeAndWriteLineDisplayed(localization.Items["gptModeText"]);

        string[] gptModes = { "local", "azure" };
        int chosen = ChooseFromList(new List<string> { localization.Items["localExplanation"], localization.Items["azureExplanation"] });
        config["gptMode"] = gptModes[chosen];
    }
    static void ChangeCaptureDevice(ref Dictionary<string, string> config)
    {
        ChangeAndWriteLineDisplayed(localization.Items["captureDeviceNameText"]);

        List<string> deviceFriendly = new();
        List<string> userFriendly = new();
        var enumerator = new MMDeviceEnumerator();
        foreach (var wasapi in enumerator.EnumerateAudioEndPoints(DataFlow.Render, DeviceState.Active))
        {
            deviceFriendly.Add(wasapi.DeviceFriendlyName);
            userFriendly.Add(wasapi.FriendlyName);
        }
        int chosen = ChooseFromList(userFriendly);
        config["captureDeviceName"] = deviceFriendly[chosen];
    }
    static int ChooseInt(int bottomLimiter = 0, int topLimiter = 1_000_000)
    {
        AddToDisplayed($"\n{localization.Items["chooseIntText"]}");

        int result = bottomLimiter;
        do
        {
            result = ChooseNumber<int>();
        } while (result < bottomLimiter || result > topLimiter);
        return result;
    }
    static float ChooseFloat(float bottomLimiter = 0, float topLimiter = 5)
    {
        AddToDisplayed($"\n{localization.Items["chooseFloatText"]}");

        float result = bottomLimiter;
        do
        {
            result = (float)ChooseNumber<double>();
        } while (result < bottomLimiter || result > topLimiter);
        return result;
    }
    static T ChooseNumber<T>() where T : IParsable<T>
    {
        T? result = default;
        while (true)
        {
            WriteLineDisplayed();
            if (T.TryParse(Console.ReadLine(), CultureInfo.InvariantCulture, out T value))
            {
                result = value;
                break;
            }
        }
        return result;
    }
    static int ChooseFromList(List<string> list)
    {
        string text = $"\n{localization.Items["chooseText"]}\n";
        for (int i = 0; i < list.Count; i++)
        {
            text+=($"\n{i + 1}. {list[i]}");
        }
        AddToDisplayed(text);
        int result;
        while (true)
        {
            WriteLineDisplayed();
            int chosen;
            try
            {
                chosen = int.Parse(Console.ReadLine());
                string temp = list[chosen - 1];
            }
            catch { continue; }
            result = chosen - 1;
            break;
        }
        return result;
    }
    static void AddToDisplayed(string str)
    {
        toDisplay += str;
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
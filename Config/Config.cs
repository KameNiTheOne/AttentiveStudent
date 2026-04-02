using NAudio.CoreAudioApi;
using Newtonsoft.Json;
using System.Globalization;
using System.Text;
using ConfigChangeReactor;

namespace ConfigDomain
{
    public static class Config
    {
        public static Localization localization;

        static Dictionary<string, string> config;

        static string pathToConfig;
        static bool isRestartRequired = false;
        static string toDisplay = "";
        public static Dictionary<string, string> Initialize()
        {
            string pathToExeFolder = AppDomain.CurrentDomain.BaseDirectory;
            localization = new Localization(pathToExeFolder);

            string pathToConfigFolder = Path.Combine(pathToExeFolder, "Settings");
            pathToConfig = Path.Combine(pathToConfigFolder, "appsettings.json");

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
            config["pathToExeFolder"] = pathToExeFolder;
            SaveConfig();
            return config;
        }
        public static Dictionary<string, string> Change(out bool restartRequired)
        {
            MainSetupMenu();
            SaveConfig();

            restartRequired = isRestartRequired;

            ReactorDomain.InvokeConfigChange(config);
            return config;
        }
        static void SaveConfig()
        {
            string configjson = JsonConvert.SerializeObject(config);
            using (var fileStream = File.Open(pathToConfig, FileMode.Create, FileAccess.Write))
            {
                using (var writer = new StreamWriter(fileStream, Encoding.UTF8))
                {
                    writer.Write(configjson);
                }
            }

            toDisplay = "";
            Console.Clear();
        }
        static Dictionary<string, string> InitialSetupMenu(Dictionary<string, string> templateConfig)
        {
            Dictionary<string, string> result = templateConfig;

            while (true)
            {
                ChangeGPTMode(out bool vCh, ref result);
                if (vCh) break;
            }
            if (result["gptMode"] == "local")
            {
                while (true)
                {
                    ChangeAndWriteLineDisplayed(localization.Items["contextSizeText"]);
                    result["contextSize"] = ChooseInt(out bool vCh).ToString();
                    if (vCh) break;
                }
                while (true)
                {
                    ChangeAndWriteLineDisplayed(localization.Items["gpuLayerCountText"]);
                    result["gpuLayerCount"] = ChooseInt(out bool vCh).ToString();
                    if (vCh) break;
                }
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
                while (true)
                {
                    ChangeAndWriteLineDisplayed(localization.Items["temperatureText"]);
                    result["temperature"] = ChooseFloat(out bool vCh).ToString();
                    if (vCh) break;
                }
            }
            while (true)
            {
                ChangeCaptureDevice(out bool vCh, ref result);
                if (vCh) break;
            }

            return result;
        }
        static void MainSetupMenu()
        {
            bool flag = true;
            while (flag)
            {
                List<string> options, keys;
                (options, keys) = MakeOptions();
                ChangeAndWriteLineDisplayed(localization.Items["mainSetupMenu"]);

                int chosen = ChooseFromList(options);
                flag = ChangersManager(keys[chosen]);
            }
        }
        static (List<string>, List<string>) MakeOptions()
        {
            List<string> result = new();
            List<string> keys = new();
            List<string> ignore;
            if (config["gptMode"] == "local")
            {
                ignore = new() { "endpoint", "apiKey", "model", "temperature", "pathToExeFolder" };
            }
            else
            {
                ignore = new() { "contextSize", "gpuLayerCount", "modelPath", "pathToExeFolder" };
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
        static bool ChangersManager(string fieldToChange)
        {
            switch (fieldToChange)
            {
                case "gptMode":
                    {
                        ChangeGPTMode(out bool valueChanged, ref config, true);
                        isRestartRequired = valueChanged;
                        return true;
                    }
                case "captureDeviceName":
                    {
                        ChangeCaptureDevice(out bool _, ref config);
                        return true;
                    }
                case "audiofileDuration":
                    {
                        ChangerOptionInt("audiofileDuration");
                        return true;
                    }
                case "endpoint":
                    {
                        ChangerOptionReadLine("endpoint");
                        return true;
                    }
                case "apiKey":
                    {
                        ChangerOptionReadLine("apiKey");
                        return true;
                    }
                case "model":
                    {
                        ChangerOptionReadLine("model");
                        return true;
                    }
                case "temperature":
                    {
                        ChangerOptionFloat("temperature");
                        return true;
                    }
                case "contextSize":
                    {
                        isRestartRequired = ChangerOptionInt("contextSize", true);
                        return true;
                    }
                case "gpuLayerCount":
                    {
                        isRestartRequired = ChangerOptionInt("gpuLayerCount", true);
                        return true;
                    }
                case "modelPath":
                    {
                        isRestartRequired = ChangerOptionReadLine("modelPath", true);
                        return true;
                    }
                case "useNotedAmount":
                    {
                        ChangerOptionInt("useNotedAmount");
                        return true;
                    }
                default:
                    {
                        return false;
                    }
            }
        }
        static bool ChangerOptionFloat(string option, bool restartWarn = false)
        {
            string toWrite = localization.Items[$"{option}Text"];
            if (restartWarn)
            {
                toWrite += $"\n{localization.Items["restartWarnText"]}";
            }
            ChangeAndWriteLineDisplayed(toWrite);
            string input = ChooseFloat(out bool valueChanged).ToString();
            if (valueChanged)
            {
                config[option] = input;
            }
            return valueChanged;
        }
        static bool ChangerOptionInt(string option, bool restartWarn = false)
        {
            string toWrite = localization.Items[$"{option}Text"];
            if (restartWarn)
            {
                toWrite += $"\n{localization.Items["restartWarnText"]}";
            }
            ChangeAndWriteLineDisplayed(toWrite);
            string input = ChooseInt(out bool valueChanged).ToString();
            if (valueChanged)
            {
                config[option] = input;
            }
            return valueChanged;
        }
        static bool ChangerOptionReadLine(string option, bool restartWarn = false)
        {
            string toWrite = $"{localization.Items[$"{option}Text"]}\n{localization.Items["cancelMenuText"]}";
            if (restartWarn)
            {
                toWrite += $"\n{localization.Items["restartWarnText"]}\n";
            }
            ChangeAndWriteLineDisplayed(toWrite);
            string input = Console.ReadLine();
            if (input.ToLower() == "c")
            {
                return false;
            }
            config[option] = input;
            return true;
        }
        static void ChangeGPTMode(out bool valueChanged, ref Dictionary<string, string> config, bool restartWarn = false)
        {
            string toWrite = localization.Items["gptModeText"];
            if (restartWarn)
            {
                toWrite += $"\n{localization.Items["restartWarnText"]}\n";
            }
            ChangeAndWriteLineDisplayed(toWrite);

            string[] gptModes = { "local", "azure", "exit"};
            
            int chosen = ChooseFromList(new List<string> { localization.Items["localExplanation"], localization.Items["azureExplanation"], localization.Items["cancelText"] });
            if (gptModes[chosen] == "exit")
            {
                valueChanged = false;
                return;
            }
            valueChanged = true;
            config["gptMode"] = gptModes[chosen];
        }
        static void ChangeCaptureDevice(out bool valueChanged, ref Dictionary<string, string> config)
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
            deviceFriendly.Add("exit");
            userFriendly.Add(localization.Items["cancelText"]);

            int chosen = ChooseFromList(userFriendly);
            if (deviceFriendly[chosen] == "exit")
            {
                valueChanged = false;
                return;
            }
            valueChanged = true;
            config["captureDeviceName"] = deviceFriendly[chosen];
        }
        static int ChooseInt(out bool valueChanged, int bottomLimiter = 0, int topLimiter = 1_000_000)
        {
            AddToDisplayed($"\n\n{localization.Items["chooseIntText"]}\n{localization.Items["cancelMenuText"]}");

            int result = bottomLimiter;
            do
            {
                result = ChooseNumber<int>(out bool vCh);
                if (vCh == false)
                {
                    valueChanged = false;
                    return 0;
                }
            } while (result < bottomLimiter || result > topLimiter);
            valueChanged = true;
            return result;
        }
        static float ChooseFloat(out bool valueChanged, float bottomLimiter = 0, float topLimiter = 5)
        {
            AddToDisplayed($"\n\n{localization.Items["chooseFloatText"]}\n{localization.Items["cancelMenuText"]}");

            float result = bottomLimiter;
            do
            {
                result = (float)ChooseNumber<double>(out bool vCh);
                if (vCh == false)
                {
                    valueChanged = false;
                    return 0;
                }
            } while (result < bottomLimiter || result > topLimiter);
            valueChanged = true;
            return result;
        }
        static T? ChooseNumber<T>(out bool valueChanged) where T : IParsable<T>
        {
            T? result = default;
            while (true)
            {
                WriteLineDisplayed();
                string userInput = Console.ReadLine();
                if (userInput.ToLower() == "c")
                {
                    valueChanged = false;
                    return result;
                }
                if (T.TryParse(userInput, CultureInfo.InvariantCulture, out T value))
                {
                    result = value;
                    break;
                }
            }
            valueChanged = true;
            return result;
        }
        static int ChooseFromList(List<string> list)
        {
            string text = $"\n{localization.Items["chooseText"]}\n";
            for (int i = 0; i < list.Count; i++)
            {
                text += ($"\n{i + 1}. {list[i]}");
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
}
using NAudio.CoreAudioApi;
using Newtonsoft.Json;
using System.Globalization;
using System.Text;
using ConfigChangeReactor;

namespace ConfigDomain
{
    public static class Config
    {
        public static Localization Localization { get; private set; }

        static Dictionary<string, string> config;

        static string pathToConfig;
        static bool isRestartRequired = false;
        static string toDisplay = "";
        public static Dictionary<string, string> Initialize()
        {
            string pathToExeFolder = AppDomain.CurrentDomain.BaseDirectory;
            string pathToConfigFolder = Path.Combine(pathToExeFolder, "Settings");
            pathToConfig = Path.Combine(pathToConfigFolder, "appsettings.json");

            if (!File.Exists(pathToConfig))
            {
                string pathToTemplate = Path.Combine(pathToConfigFolder, "appsettingsTemplate.json");
                var templateConfig = Instruments.DeserializeObjectFromFile<Dictionary<string, string>>(pathToTemplate);

                Localization = new Localization(pathToExeFolder);

                config = InitialSetupMenu(templateConfig);
            }
            else
            {
                config = Instruments.DeserializeObjectFromFile<Dictionary<string, string>>(pathToConfig);

                Localization = new Localization(pathToExeFolder, config["localizationLang"], config["transcriberLang"]);
            }
            
            config["pathToExeFolder"] = pathToExeFolder;
            SaveConfig();

            return config;
        }
        public static Dictionary<string, string> Change(out bool restartRequired)
        {
            MainSetupMenu();

            restartRequired = isRestartRequired;

            ReactorDomain.InvokeConfigChange(config);

            SaveConfig();
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
                    ChangeAndWriteLineDisplayed(Localization.Items["contextSizeText"]);
                    result["contextSize"] = ChooseInt(out bool vCh).ToString();
                    if (vCh) break;
                }
                
                while (true)
                {
                    ChangeAndWriteLineDisplayed(Localization.Items["gpuLayerCountText"]);
                    result["gpuLayerCount"] = ChooseInt(out bool vCh).ToString();
                    if (vCh) break;
                }
                
                ChangeAndWriteLineDisplayed(Localization.Items["modelPathText"]);
                result["modelPath"] = Console.ReadLine();
            }
            else
            {
                ChangeAndWriteLineDisplayed(Localization.Items["endpointText"]);
                result["endpoint"] = Console.ReadLine();
                
                ChangeAndWriteLineDisplayed(Localization.Items["apiKeyText"]);
                result["apiKey"] = Console.ReadLine();
                
                ChangeAndWriteLineDisplayed(Localization.Items["modelText"]);
                result["model"] = Console.ReadLine();
                
                while (true)
                {
                    ChangeAndWriteLineDisplayed(Localization.Items["temperatureText"]);
                    result["temperature"] = ChooseFloat(out bool vCh).ToString();
                    if (vCh) break;
                }
            }
            while (true)
            {
                ChangeCaptureDevice(out bool vCh, ref result);
                if (vCh) break;
            }

            result["localizationLang"] = Localization.Language;
            result["transcriberLang"] = Localization.TranscriberLanguage;

            return result;
        }
        static void MainSetupMenu()
        {
            bool flag = true;
            while (flag)
            {
                List<string> options, keys;
                (options, keys) = MakeOptions();
                ChangeAndWriteLineDisplayed(Localization.Items["mainSetupMenu"]);

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
                result.Add($"{key}\n   {Localization.Items["currentText"]} {config[key]}\n");
                keys.Add(key);
            }
            result.Add(Localization.Items["exitOption"]);
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
                        break;
                    }
                case "captureDeviceName":
                    {
                        ChangeCaptureDevice(out bool _, ref config);
                        break;
                    }
                case "audiofileDuration":
                    {
                        ChangeOptionInt("audiofileDuration");
                        break;
                    }
                case "endpoint":
                    {
                        ChangeOptionReadLine("endpoint");
                        break;
                    }
                case "apiKey":
                    {
                        ChangeOptionReadLine("apiKey");
                        break;
                    }
                case "model":
                    {
                        ChangeOptionReadLine("model");
                        break;
                    }
                case "temperature":
                    {
                        ChangeOptionFloat("temperature");
                        break;
                    }
                case "contextSize":
                    {
                        ChangeOptionInt("contextSize", true);
                        break;
                    }
                case "gpuLayerCount":
                    {
                        ChangeOptionInt("gpuLayerCount", true);
                        break;
                    }
                case "modelPath":
                    {
                        ChangeOptionReadLine("modelPath", true);
                        break;
                    }
                case "useNotedAmount":
                    {
                        ChangeOptionInt("useNotedAmount");
                        break;
                    }
                case "localizationLang":
                    {
                        ChangeLocalizationLanguage();
                        break;
                    }
                case "transcriberLang":
                    {
                        ChangeTranscriberLanguage();
                        break;
                    }
                default:
                    {
                        return false;
                    }
            }
            return true;
        }
        static void ChangeOptionFloat(string option, bool restartOnChange = false)
        {
            string toWrite = Localization.Items[$"{option}Text"];
            if (restartOnChange)
            {
                toWrite += $"\n{Localization.Items["restartWarnText"]}";
            }
            ChangeAndWriteLineDisplayed(toWrite);
            
            string input = ChooseFloat(out bool valueChanged).ToString();
            if (valueChanged)
            {
                config[option] = input;
                isRestartRequired = restartOnChange;
            }
        }
        static void ChangeOptionInt(string option, bool restartOnChange = false)
        {
            string toWrite = Localization.Items[$"{option}Text"];
            if (restartOnChange)
            {
                toWrite += $"\n{Localization.Items["restartWarnText"]}";
            }
            ChangeAndWriteLineDisplayed(toWrite);
            
            string input = ChooseInt(out bool valueChanged).ToString();
            if (valueChanged)
            {
                config[option] = input;
                isRestartRequired = restartOnChange;
            }
        }
        static void ChangeOptionReadLine(string option, bool restartOnChange = false)
        {
            string toWrite = $"{Localization.Items[$"{option}Text"]}\n{Localization.Items["cancelMenuText"]}";
            if (restartOnChange)
            {
                toWrite += $"\n{Localization.Items["restartWarnText"]}\n";
            }
            ChangeAndWriteLineDisplayed(toWrite);
            
            string input = Console.ReadLine();
            if (input.ToLower() == "c")
            {
                return;
            }
            
            config[option] = input;
            isRestartRequired = restartOnChange;
        }
        static void ChangeGPTMode(out bool valueChanged, ref Dictionary<string, string> config, bool restartWarn = false)
        {
            string toWrite = Localization.Items["gptModeText"];
            if (restartWarn)
            {
                toWrite += $"\n{Localization.Items["restartWarnText"]}\n";
            }
            ChangeAndWriteLineDisplayed(toWrite);

            string[] gptModes = { "local", "azure", "exit"};
            
            int chosen = ChooseFromList(new List<string> { Localization.Items["localExplanation"], Localization.Items["azureExplanation"], Localization.Items["cancelText"] });
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
            ChangeAndWriteLineDisplayed(Localization.Items["captureDeviceNameText"]);

            List<string> deviceFriendly = new();
            List<string> userFriendly = new();
            var enumerator = new MMDeviceEnumerator();
            foreach (var wasapi in enumerator.EnumerateAudioEndPoints(DataFlow.Render, DeviceState.Active))
            {
                deviceFriendly.Add(wasapi.DeviceFriendlyName);
                userFriendly.Add(wasapi.FriendlyName);
            }
            deviceFriendly.Add("exit");
            userFriendly.Add(Localization.Items["cancelText"]);

            int chosen = ChooseFromList(userFriendly);
            if (deviceFriendly[chosen] == "exit")
            {
                valueChanged = false;
                return;
            }
            
            valueChanged = true;
            config["captureDeviceName"] = deviceFriendly[chosen];
        }
        static void ChangeLocalizationLanguage()
        {
            string toWrite = $"{Localization.Items["localizationLangText"]}\n{Localization.Items["cancelMenuText"]}";
            ChangeAndWriteLineDisplayed(toWrite);

            string input = Console.ReadLine();
            if (input.ToLower() == "c")
            {
                return;
            }

            bool changed = Localization.ChangeLocalization(input);
            if (!changed)
            {
                ChangeAndWriteLineDisplayed(Localization.Items["localizationLangFailText"]);
                Console.ReadLine();
                return;
            }
            
            config["localizationLang"] = input;
        }
        static void ChangeTranscriberLanguage()
        {
            ChangeAndWriteLineDisplayed(Localization.Items["transcriberLangText"]);

            List<string> iSOLangs = Localization.AvailableLanguages.Keys.ToList();
            int chosen = ChooseFromList(iSOLangs);

            config["transcriberLang"] = Localization.ConvertLanguageNameFromISOToTranscriber(iSOLangs[chosen]);
        }
        static int ChooseInt(out bool valueChanged, int bottomLimiter = 0, int topLimiter = 1_000_000)
        {
            AddToDisplayed($"\n\n{Localization.Items["chooseIntText"]}\n{Localization.Items["cancelMenuText"]}");

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
            AddToDisplayed($"\n\n{Localization.Items["chooseFloatText"]}\n{Localization.Items["cancelMenuText"]}");

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
            string text = $"\n{Localization.Items["chooseText"]}\n";
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
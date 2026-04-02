using System.Globalization;
namespace ConfigDomain
{
    public class Localization
    {
        public Dictionary<string, string> Items;
        public Localization(string pathToExeFolder)
        {
            string ISOLanguage = CultureInfo.InstalledUICulture.ThreeLetterISOLanguageName;

            string pathToConfigFolder = Path.Combine(pathToExeFolder, "Settings");
            string pathToLanguages = Path.Combine(pathToConfigFolder, "languages.json");
            string language = ConvertLanguageNameFromISOToTranscriber(ISOLanguage, pathToLanguages);
            Items = GetLocalization(pathToConfigFolder, language);

            string pathToTranscriberFolder = Path.Combine(pathToExeFolder, "Transcriber");
            string pathToApp = Path.Combine(pathToTranscriberFolder, "app");
            string pathToLanguageFile = Path.Combine(pathToApp, "language.txt");
            CreateTransciberLanguageFile(pathToLanguageFile, language);
        }
        string ConvertLanguageNameFromISOToTranscriber(string ISOLanguage, string pathToLanguages)
        {
            var languages = Instruments.DeserializeObjectFromFile<Dictionary<string, string>>(pathToLanguages);
            if (languages.TryGetValue(ISOLanguage, out string value))
            {
                return value;
            }
            return "english";
        }
        Dictionary<string, string> GetLocalization(string pathToLocalizationFolder, string language)
        {
            string pathToLocalization = Path.Combine(pathToLocalizationFolder, $"{language}.json");
            return Instruments.DeserializeObjectFromFile<Dictionary<string, string>>(pathToLocalization);
        }
        void CreateTransciberLanguageFile(string path, string language)
        {
            using (var stream = File.Open(path, FileMode.Create))
            {
                using (var writer = new StreamWriter(stream))
                {
                    writer.Write(language);
                }
            }
        }
    }
}

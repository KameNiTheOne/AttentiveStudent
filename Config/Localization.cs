using ConfigChangeReactor;
using System.Globalization;
namespace ConfigDomain
{
    public class Localization
    {

        public Dictionary<string, string> Items;
        public string TranscriberLanguage { get; private set; }
        public string Language { get; private set; }
        public Dictionary<string, string> AvailableLanguages { get; private set; }

        string defaultISOLanguage = "eng";
        string pathToLocalizationFolder;
        public Localization(string pathToExeFolder, string localizationLanguage = "", string transcriberLanguage = "")
        {
            if (localizationLanguage == "")
            {
                Language = CultureInfo.InstalledUICulture.ThreeLetterISOLanguageName;
            }
            else
            {
                Language = localizationLanguage;
            }

            pathToLocalizationFolder = Path.Combine(pathToExeFolder, "Settings");
            string pathToLanguages = Path.Combine(pathToLocalizationFolder, "languages.json");
            AvailableLanguages = Instruments.DeserializeObjectFromFile<Dictionary<string, string>>(pathToLanguages);
            AvailableLanguages["auto"] = "auto";

            if (transcriberLanguage == "")
            {
                TranscriberLanguage = ConvertLanguageNameFromISOToTranscriber(Language);
            }
            else
            {
                TranscriberLanguage = transcriberLanguage;
            }

            Items = getLocalization(Language, out bool localizationForISOExists);

            if (localizationForISOExists)
            {
                defaultISOLanguage = Language;
            }
        }
        public string ConvertLanguageNameFromISOToTranscriber(string language)
        {
            if (AvailableLanguages.TryGetValue(language, out string value))
            {
                return value;
            }
            return "auto";
        }
        Dictionary<string, string> getLocalization(string language, out bool localizationForISOExists)
        {
            localizationForISOExists = true;
            string pathToLocalization = Path.Combine(pathToLocalizationFolder, $"{language}.json");
            string pathToDefaultLocalization = Path.Combine(pathToLocalizationFolder, $"{defaultISOLanguage}.json");

            Dictionary<string, string> items;
            try
            {
                items = Instruments.DeserializeObjectFromFile<Dictionary<string, string>>(pathToLocalization);
            }
            catch (FileNotFoundException)
            {
                items = Instruments.DeserializeObjectFromFile<Dictionary<string, string>>(pathToDefaultLocalization);
                localizationForISOExists = false;
            }
            return items;
        }
        public bool ChangeLocalization(string localizationLanguage)
        {
            Items = getLocalization(localizationLanguage, out bool localizationForISOExists);
            if (localizationForISOExists)
            {
                Language = localizationLanguage;
            }

            return localizationForISOExists;
        }
    }
}

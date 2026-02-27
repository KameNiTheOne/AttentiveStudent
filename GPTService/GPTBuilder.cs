namespace GPTService
{
    public class GPTBuilder
    {
        Dictionary<string, string> settings;
        Dictionary<string, string> prompts;
        public GPTBuilder(Dictionary<string, string> _settings, Dictionary<string, string> _prompts)
        {
            settings = _settings;
            prompts = _prompts;
        }
        public IGPT Build()
        {
            IGPT gpt;
            if (settings["gptMode"] == "local")
            {
                gpt = new LocalGPT(new LocalGPTSettings(settings, prompts));
            }
            else
            {
                gpt = new AzureGPT(new AzureGPTSettings(settings, prompts));
            }
            return gpt;
        }
    }
}

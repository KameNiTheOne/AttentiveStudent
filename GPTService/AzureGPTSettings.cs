using ConfigChangeReactor;

namespace GPTService
{
    public class AzureGPTSettings : Configurable
    {
        public AzureGPTSettings(Dictionary<string, string> cnfg, Dictionary<string, string> prompts)
        {
            Endpoint = cnfg["endpoint"];
            APIKey = cnfg["apiKey"];
            Model = cnfg["model"];
            Temperature = float.Parse(cnfg["temperature"]);
            Prompts = prompts;

            ReactorDomain.Subscribe(ChangeHandler);
        }
        public string? Endpoint { get; set; }
        public string? APIKey { get; set; }
        public string? Model { get; set; }
        public float Temperature { get; set; }
        public Dictionary<string, string> Prompts { get; set; }
        public override void ChangeHandler(Dictionary<string, string> cnfg)
        {
            Endpoint = cnfg["endpoint"];
            APIKey = cnfg["apiKey"];
            Model = cnfg["model"];
            Temperature = float.Parse(cnfg["temperature"]);
        }
    }
}
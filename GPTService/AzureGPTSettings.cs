using Azure;
using Azure.AI.Inference;

namespace GPTService
{
    public class AzureGPTSettings
    {
        public AzureGPTSettings(Dictionary<string, string> cnfg, Dictionary<string, string> prompts)
        {
            Endpoint = cnfg["endpoint"];
            APIKey = cnfg["apiKey"];
            Model = cnfg["model"];
            Temperature = float.Parse(cnfg["temperature"]);
            Prompts = prompts;

            if (!isValid(out string errorMessage))
            {
                throw new Exception(errorMessage);
            }
        }
        public string? Endpoint { get; set; }
        public string? APIKey { get; set; }
        public string? Model { get; set; }
        public float Temperature { get; set; }
        public Dictionary<string, string> Prompts { get; set; }
        bool isValid(out string errorMessage)
        {
            try
            {
                ChatCompletionsClient client = new ChatCompletionsClient(
                    new Uri(Endpoint),
                    new AzureKeyCredential(APIKey),
                    new AzureAIInferenceClientOptions());
            }
            catch (Exception ex)
            {
                errorMessage = ex.Message;
                return false;
            }
            if (string.IsNullOrWhiteSpace(APIKey))
            {
                errorMessage = "Invalid API key";
                return false;
            }
            if (string.IsNullOrWhiteSpace(Model))
            {
                errorMessage = "Invalid model";
                return false;
            }
            errorMessage = "";
            return true;
        }
    }
}
using Azure;
using Azure.AI.Inference;

namespace GPTService
{
    public class AzureGPT : IGPT
    {
        AzureGPTSettings settings;

        ChatCompletionsClient client;
        public AzureGPT(AzureGPTSettings _settings)
        {
            settings = _settings;

            client = new ChatCompletionsClient(
                new Uri(settings.Endpoint),
                new AzureKeyCredential(settings.APIKey),
                new AzureAIInferenceClientOptions());
        }
        public async Task<string> Query(string query, CancellationToken ct)
        {
            var requestOptions = new ChatCompletionsOptions()
            {
                Messages =
                {
                    new ChatRequestSystemMessage(settings.Prompts["gptSystemPrompt"]),
                    new ChatRequestUserMessage(query),
                },
                Temperature = settings.Temperature,
                NucleusSamplingFactor = 1.0f,
                MaxTokens = 4096,
                Model = settings.Model,
            };

            try
            {
                Response<ChatCompletions> response = await client.CompleteAsync(requestOptions, ct);
                string res = response.Value.Content;
                return res;
            }
            catch (TaskCanceledException)
            {
                return "";
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.ToString());
                await Task.Delay(3000);
                return "";
            }
        }
        ~AzureGPT()
        {
            settings.Dispose().Wait();
        }
    }
}
using LLama;
using LLama.Common;
using LLama.Sampling;
using System.Text.RegularExpressions;

namespace GPTService
{
    public class LocalGPT : IGPT
    {
        LocalGPTSettings settings;

        const string patternToTrim = @"(\bUser\W)|(\bAssistant\W)|(\bSystem\W)";

        InferenceParams? inferenceParams;
        SessionState resetState;
        ChatSession mainsession;
        public LocalGPT(LocalGPTSettings _settings)
        {
            settings = _settings;

            ModelParams parameters = new ModelParams(settings.ModelPath)
            {
                ContextSize = settings.ContextSize, // The longest length of chat as memory.
                GpuLayerCount = settings.GpuLayerCount // How many layers to offload to GPU. Please adjust it according to your GPU memory.
            };

            LLamaWeights model = LLamaWeights.LoadFromFile(parameters);
            LLamaContext context = model.CreateContext(parameters);

            InteractiveExecutor executor = new InteractiveExecutor(context);

            ChatHistory new_history = new();
            resetState = new ChatSession(executor, new_history).GetSessionState();

            mainsession = new(executor, new_history);

            inferenceParams = new InferenceParams()
            {
                SamplingPipeline = new DefaultSamplingPipeline() { Temperature = 0.75f },
                AntiPrompts = new List<string> { "User:", "System:", "User: ", "System: ", "\nUser:", "\nSystem:", "\nUser: ", "\nSystem: " }, // Stop generation once antiprompts appear.
                MaxTokens = 256 // No more than 256 tokens should appear in answer. Remove it if antiprompt is enough for control.
            };
        }
        /// <summary>
        /// Removes unnecessary patterns specified in patternToTrim and whitespace characters from start and end of string (if trimWhiteSpace is true).
        /// </summary>
        string cleanResponse(string response, bool trimWhiteSpace = true)
        {
            string regexedResponse = Regex.Replace(response, patternToTrim, string.Empty);
            if (trimWhiteSpace)
            {
                regexedResponse = regexedResponse.TrimStart().TrimEnd();
            }
            return regexedResponse;
        }
        public async Task<string> Query(string query, CancellationToken ct)
        {
            mainsession.LoadSession(resetState);

            mainsession.AddMessage(new ChatHistory.Message(AuthorRole.System, settings.Prompts["gptSystemPrompt"]));
            mainsession.AddMessage(new ChatHistory.Message(AuthorRole.User, settings.Prompts["gptUserPrompt"]));
            mainsession.AddMessage(new ChatHistory.Message(AuthorRole.Assistant, settings.Prompts["gptAssistantPrompt"]));

            string res = "";
            try
            {
                await foreach (string generated in mainsession.ChatAsync(new ChatHistory.Message(AuthorRole.User, query), inferenceParams, ct))
                {
                    res += generated;
                }
            }
            catch (TaskCanceledException)
            {
                return "";
            }

            return cleanResponse(res);
        }
    }
}

namespace GPTService
{
    public class LocalGPTSettings
    {
        public LocalGPTSettings(Dictionary<string, string> cnfg, Dictionary<string, string> prompts)
        {
            ContextSize = uint.Parse(cnfg["contextSize"]);
            GpuLayerCount = int.Parse(cnfg["gpuLayerCount"]);
            ModelPath = cnfg["modelPath"];
            Prompts = prompts;
        }
        public uint ContextSize { get; set; }
        public int GpuLayerCount { get; set; }
        public string ModelPath { get; set; }
        public Dictionary<string, string> Prompts { get; set; }
    }
}

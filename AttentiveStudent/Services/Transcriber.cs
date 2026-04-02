using CaptureService.Instruments;
using Newtonsoft.Json;

namespace CaptureService.Services
{
    class Transcribed
    {
        [JsonProperty("text")]
        public string Value { get; set; }
        public Transcribed(string value)
        {
            Value = value;
        }
    }
    public class Transcriber : Containerable
    {
        HttpRetriever http;
        int port = 1779;

        private Transcriber() {}
        async public static Task<Transcriber> Initialize(Dictionary<string, string> cnfg)
        {
            var instance = new Transcriber();
            instance.http = new HttpRetriever();

            instance.port = await instance.InitializeContainer(Path.Combine(cnfg["pathToExeFolder"], "Transcriber"));
            return instance;
        }
        public async Task<string> Transcribe(string pathToTempFile, int duration, CancellationToken ct)
        {
            string postResult = await http.PostAudiofileAsync($"http://localhost:{port}", pathToTempFile, duration, ct);
            File.Delete(pathToTempFile);
            return DeserializeTranscribed(postResult).Value;
        }
        Transcribed DeserializeTranscribed(string jsonObj)
        {
            Transcribed transcribed;
            if (jsonObj == "\"\"")
            {
                transcribed = new Transcribed("");
            }
            else
            {
                transcribed = JsonConvert.DeserializeObject<Transcribed>(jsonObj);
            }
            return transcribed;
        }
    }
}

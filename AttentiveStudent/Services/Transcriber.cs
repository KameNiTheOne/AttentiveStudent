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
        string pathToExeFolder;
        int audiofileDuration;
        int port = 1779;

        private Transcriber() {}
        async public static Task<Transcriber> Initialize(Dictionary<string, string> cnfg)
        {
            var instance = new Transcriber();
            instance.http = new HttpRetriever();
            instance.pathToExeFolder = cnfg["pathToExeFolder"];
            instance.audiofileDuration = int.Parse(cnfg["audiofileDuration"]);

            instance.port = await instance.InitializeContainer(Path.Combine(instance.pathToExeFolder, "Transcriber"));
            return instance;
        }
        public async Task<string> Transcribe(string pathToTempFile, CancellationToken ct)
        {
            string postResult = await http.PostAudiofileAsync($"http://localhost:{port}", pathToTempFile, audiofileDuration, ct);
            File.Delete(pathToTempFile);
            return DeserializeTranscribed(postResult).Value;
        }
        public async Task<string> TranscribeMultiple(string[] audiofilesPaths, int lastFileDuration, CancellationToken ct)
        {
            int filesAmount = audiofilesPaths.Length;

            (string, int)[] audiofiles = new (string, int)[filesAmount];
            for (int i = 0; i < filesAmount; ++i)
            {
                audiofiles[i] = (Path.Combine(pathToExeFolder, $"notedTemp{i}.wav"), audiofileDuration);
                File.Copy(audiofilesPaths[i], audiofiles[i].Item1);
            }
            audiofiles[filesAmount-1] = (audiofiles[filesAmount-1].Item1, lastFileDuration);

            string postResult = "";
            foreach (var file in audiofiles)
            {
                string temp = await http.PostAudiofileAsync($"http://localhost:{port}", file.Item1, file.Item2, ct);
                postResult += DeserializeTranscribed(temp).Value;
                File.Delete(file.Item1);
            }

            return postResult;
        }
        Transcribed DeserializeTranscribed(string jsonObj)
        {
            Transcribed transcribed;
            if (String.IsNullOrEmpty(jsonObj))
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

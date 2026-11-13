using Newtonsoft.Json;
using System.Net.Http.Headers;

namespace CaptureService.Instruments
{
    class Information
    {
        [JsonProperty("duration")]
        public int Duration { get; set; }
        [JsonProperty("language")]
        public string Language { get; set; }
        public Information(int duration, string language)
        {
            Duration = duration;
            Language = language;
        }
    }
    public class HttpRetriever
    {
        HttpClient client;
        public HttpRetriever()
        {
            client = new HttpClient() { Timeout = TimeSpan.FromSeconds(600) };
        }
        MultipartFormDataContent PrepareAudiofile(string filePath)
        {
            var multipartFormContent = new MultipartFormDataContent();
            byte[] fileToBytes = File.ReadAllBytes(filePath);
            File.Delete(filePath);

            var content = new ByteArrayContent(fileToBytes);

            content.Headers.ContentType = new MediaTypeHeaderValue("audio/wav");
            multipartFormContent.Add(content, name: "audiofile", fileName: "audio.mp3");

            return multipartFormContent;
        }
        async Task PostInformation(string http, int duration, string language, CancellationToken ct)
        {
            using var jsonContent = new StringContent(JsonConvert.SerializeObject(new Information(duration, language)));
            jsonContent.Headers.ContentType = new MediaTypeHeaderValue("application/json");

            try
            {
                using var response = await client.PostAsync($"{http}/sendinformation/", jsonContent, ct);
                _ = await response.Content.ReadAsStringAsync(ct);
            }
            catch (TaskCanceledException) { }
        }
        public async Task<string> PostAudiofileAsync(string http, string filePath, int duration, string language, CancellationToken ct)
        {
            await PostInformation(http, duration, language, ct);

            using var multipartFormContent = PrepareAudiofile(filePath);

            string result;
            try
            {
                using var response = await client.PostAsync($"{http}/transcribe/", multipartFormContent, ct);
                string responseText = await response.Content.ReadAsStringAsync(ct);

                result = responseText;
            }
            catch (TaskCanceledException)
            {
                return "";
            }

            return result;
        }
    }
}

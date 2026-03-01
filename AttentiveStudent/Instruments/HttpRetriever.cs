using Newtonsoft.Json;
using System.Net.Http.Headers;

namespace CaptureService.Instruments
{
    class Duration
    {
        [JsonProperty("amount")]
        public int Amount { get; set; }
        public Duration(int amount)
        {
            Amount = amount;
        }
    }
    public class HttpRetriever
    {
        HttpClient client;
        public HttpRetriever()
        {
            client = new HttpClient() { Timeout = TimeSpan.FromSeconds(600) };
        }
        MultipartFormDataContent prepareMultipartFormContent(string filePath, string fileName)
        {
            var multipartFormContent = new MultipartFormDataContent();
            byte[] fileToBytes = File.ReadAllBytes(filePath);
            var content = new ByteArrayContent(fileToBytes);

            content.Headers.ContentType = new MediaTypeHeaderValue("audio/wav");
            multipartFormContent.Add(content, name: "audiofile", fileName: fileName);

            return multipartFormContent;
        }
        async Task postDuration(string http, int amount, CancellationToken ct)
        {
            using var jsonContent = new StringContent(JsonConvert.SerializeObject(new Duration(amount)));
            jsonContent.Headers.ContentType = new MediaTypeHeaderValue("application/json");

            try
            {
                using var response = await client.PostAsync($"{http}/sendduration/", jsonContent, ct);
                _ = await response.Content.ReadAsStringAsync(ct);
            }
            catch (TaskCanceledException) { }
        }
        public async Task<string> PostAudiofileAsync(string http, string filePath, int duration, CancellationToken ct)
        {
            await postDuration(http, duration, ct);

            using var multipartFormContent = prepareMultipartFormContent(filePath, "audio.mp3");

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

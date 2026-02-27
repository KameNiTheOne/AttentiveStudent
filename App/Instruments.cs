using Newtonsoft.Json;

namespace AttentiveStudent
{
    public static class Instruments
    {
        public static T DeserializeObjectFromFile<T>(string pathToFile)
        {
            string stringObject;
            using (var stream = File.Open(pathToFile, FileMode.Open))
            {
                using (var reader = new StreamReader(stream))
                {
                    stringObject = reader.ReadToEnd();
                }
            }
            return JsonConvert.DeserializeObject<T>(stringObject);
        }
    }
}

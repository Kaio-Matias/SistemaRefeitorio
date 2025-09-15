using System.Net.Http.Headers;
using System.Text;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace ApiRefeicoes.Services
{
    public class FaceApiService
    {
        private readonly HttpClient _httpClient;
        private readonly string _apiKey;
        private readonly string _apiEndpoint;

        public async Task<(bool IsIdentical, double Confidence)> VerifyFaces(string faceId1, string faceId2)
        {
            _httpClient.DefaultRequestHeaders.Clear();
            _httpClient.DefaultRequestHeaders.Add("Ocp-Apim-Subscription-Key", _apiKey);

            var requestUrl = $"{_apiEndpoint}/face/v1.0/verify";

            var requestBody = new { faceId1, faceId2 };
            var jsonBody = JsonConvert.SerializeObject(requestBody);

            using var content = new StringContent(jsonBody, Encoding.UTF8, "application/json");

            var response = await _httpClient.PostAsync(requestUrl, content);

            if (response.IsSuccessStatusCode)
            {
                var responseString = await response.Content.ReadAsStringAsync();
                var result = JObject.Parse(responseString);
                var isIdentical = result["isIdentical"]?.Value<bool>() ?? false;
                var confidence = result["confidence"]?.Value<double>() ?? 0.0;
                return (isIdentical, confidence);
            }

            return (false, 0.0);
        }
        public FaceApiService(IConfiguration configuration)
        {
            _httpClient = new HttpClient();
            _apiKey = configuration["AzureFaceAPI:ApiKey"];
            _apiEndpoint = configuration["AzureFaceAPI:Endpoint"];
        }

        public async Task<string?> DetectFaceAndGetId(byte[] imageBytes)
        {
            _httpClient.DefaultRequestHeaders.Clear();
            _httpClient.DefaultRequestHeaders.Add("Ocp-Apim-Subscription-Key", _apiKey);

            var requestUrl = $"{_apiEndpoint}/face/v1.0/detect?returnFaceId=true&detectionModel=detection_03";

            using var content = new ByteArrayContent(imageBytes);
            content.Headers.ContentType = new MediaTypeHeaderValue("application/octet-stream");

            var response = await _httpClient.PostAsync(requestUrl, content);

            if (response.IsSuccessStatusCode)
            {
                var responseString = await response.Content.ReadAsStringAsync();
                var faces = JArray.Parse(responseString);
                if (faces.Count > 0)
                {
                    return faces[0]["faceId"]?.ToString();
                }
            }

            return null;
        }
    }
}
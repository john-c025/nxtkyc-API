using Newtonsoft.Json.Linq;
using Newtonsoft.Json;
using System.Text;

namespace CoreHRAPI.Handlers
{
    public class GeminiHelper
    {
        private readonly HttpClient _httpClient;
        private readonly ILogger<GeminiHelper> _logger;
        private readonly string _apiKey;

        public GeminiHelper(HttpClient httpClient, IConfiguration config, ILogger<GeminiHelper> logger)
        {
            _httpClient = httpClient;
            _apiKey = config["Gemini:APIKey"];
            _logger = logger;
        }

        public async Task<string> GetBestSpecAreaMatch(string address, List<string> areaList)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(address) || areaList == null || !areaList.Any())
                {
                    _logger.LogWarning("Address or area list is missing. Skipping match.");
                    return null;
                }

                string listAsString = string.Join(", ", areaList.Distinct());
                string prompt = $"You are given a Philippine address and a list of possible specific areas. " +
                                $"Match the address to the most appropriate specific area. Be strict and avoid guessing. " +
                                $"-- Address for matching: {address} -- List to get area to match to: {listAsString}";

                var payload = new
                {
                    contents = new[]
                    {
                        new
                        {
                            parts = new[] { new { text = prompt } }
                        }
                    }
                };

                var response = await _httpClient.PostAsync(
                    $"https://generativelanguage.googleapis.com/v1beta/models/gemini-2.0-flash:generateContent?key={_apiKey}",
                    new StringContent(JsonConvert.SerializeObject(payload), Encoding.UTF8, "application/json")
                );

                if (!response.IsSuccessStatusCode)
                {
                    var errorContent = await response.Content.ReadAsStringAsync();
                    _logger.LogError("Gemini API call failed. Status: {Status}, Response: {Content}", response.StatusCode, errorContent);
                    return null;
                }

                var content = await response.Content.ReadAsStringAsync();
                var parsed = JObject.Parse(content);
                string match = parsed["candidates"]?[0]?["content"]?["parts"]?[0]?["text"]?.ToString()?.Trim();

                if (string.IsNullOrWhiteSpace(match))
                {
                    _logger.LogWarning("Gemini returned no valid match for address: {Address}", address);
                }

                return match;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error calling Gemini API for address '{address}'");
                return null;
            }
        }


        public async Task<string> GenerateGeminiContentAsync(string prompt)
        {
            try
            {
                var payload = new
                {
                    contents = new[]
                    {
                        new
                        {
                            parts = new[] { new { text = prompt } }
                        }
                    }
                };

                var response = await _httpClient.PostAsync(
                    $"https://generativelanguage.googleapis.com/v1beta/models/gemini-2.0-flash:generateContent?key={_apiKey}",
                    new StringContent(JsonConvert.SerializeObject(payload), Encoding.UTF8, "application/json")
                );

                if (!response.IsSuccessStatusCode)
                {
                    var errorContent = await response.Content.ReadAsStringAsync();
                    _logger.LogError("Gemini API call failed. Status: {Status}, Response: {Content}", response.StatusCode, errorContent);
                    return null;
                }

                var content = await response.Content.ReadAsStringAsync();
                var parsed = JObject.Parse(content);
                return parsed["candidates"]?[0]?["content"]?["parts"]?[0]?["text"]?.ToString()?.Trim();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generating content from Gemini.");
                return null;
            }
        }
        public async Task<string> GenerateGeminiRawResponseAsync(string prompt)
        {
            try
            {
                var payload = new
                {
                    contents = new[]
                    {
                new
                {
                    parts = new[] { new { text = prompt } }
                }
            }
                };

                var response = await _httpClient.PostAsync(
                    $"https://generativelanguage.googleapis.com/v1beta/models/gemini-2.0-flash:generateContent?key={_apiKey}",
                    new StringContent(JsonConvert.SerializeObject(payload), Encoding.UTF8, "application/json")
                );

                if (!response.IsSuccessStatusCode)
                {
                    var error = await response.Content.ReadAsStringAsync();
                    _logger.LogError("Gemini API error: {error}", error);
                    return null;
                }

                return await response.Content.ReadAsStringAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Gemini error.");
                return null;
            }
        }

        public async Task<string> GenerateGeminiFromStructuredPayloadAsync(object structuredPayload)
        {
            try
            {
                var json = JsonConvert.SerializeObject(structuredPayload);
                var response = await _httpClient.PostAsync(
                    $"https://generativelanguage.googleapis.com/v1beta/models/gemini-2.0-flash:generateContent?key={_apiKey}",
                    new StringContent(json, Encoding.UTF8, "application/json")
                );

                if (!response.IsSuccessStatusCode)
                {
                    string err = await response.Content.ReadAsStringAsync();
                    _logger.LogError("Gemini API failed: {err}", err);
                    return null;
                }

                return await response.Content.ReadAsStringAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Structured Gemini call failed.");
                return null;
            }
        }


    }
}

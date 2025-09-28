using System;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Extensions.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using MyTelegram.Messenger.Services.Interfaces;
using Newtonsoft.Json;

namespace MyTelegram.Messenger.Services.Impl;

public class CustomVerificationCodeGenerator : IVerificationCodeGenerator, ITransientDependency
{
    private readonly IOptionsMonitor<MyTelegramMessengerServerOptions> _options;
    private readonly IRandomHelper _randomHelper;
    private readonly ILogger<CustomVerificationCodeGenerator> _logger;
    private readonly HttpClient _httpClient;
    private readonly string _phpApiBaseUrl;
    private static readonly Dictionary<string, string> _phoneCodeCache = new();
    private const int MaxCacheSize = 1000;
    private const int HttpTimeoutSeconds = 10;

    public CustomVerificationCodeGenerator(
        IOptionsMonitor<MyTelegramMessengerServerOptions> options,
        IRandomHelper randomHelper,
        ILogger<CustomVerificationCodeGenerator> logger,
        IHttpClientFactory httpClientFactory)
    {
        _options = options;
        _randomHelper = randomHelper;
        _logger = logger;
        _httpClient = httpClientFactory.CreateClient();
        _httpClient.Timeout = TimeSpan.FromSeconds(HttpTimeoutSeconds);
        _phpApiBaseUrl = options.CurrentValue.CustomPhpApiUrl ?? "https://opengra.me/api/";
    }

    public string Generate()
    {
        var randomPhone = "+99999" + _randomHelper.GenerateRandomNumber(7);
        return Generate(randomPhone);
    }

    public string Generate(string phoneNumber)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(phoneNumber) || phoneNumber.Length > 15)
            {
                return GenerateDefault();
            }

            if (!_options.CurrentValue.UsePhpApiForCodes)
            {
                return GenerateDefault();
            }
            CleanupCache();
            StorePhoneNumber(phoneNumber);
            var code = GenerateFromPhpApiAsync(phoneNumber).GetAwaiter().GetResult();
            if (!string.IsNullOrEmpty(code) && code.All(char.IsDigit) && code.Length == 5)
            {
                _phoneCodeCache[phoneNumber] = code;
                return code;
            }
            return GenerateDefault();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating code for phone {PhoneNumber}", phoneNumber);
            return GenerateDefault();
        }
    }
    
    private void CleanupCache()
    {
        if (_phoneCodeCache.Count > MaxCacheSize)
        {
            var keysToRemove = _phoneCodeCache.Keys.Take(_phoneCodeCache.Count - MaxCacheSize / 2).ToList();
            foreach (var key in keysToRemove)
            {
                _phoneCodeCache.Remove(key);
            }
        }
    }
    
    private void StorePhoneNumber(string phoneNumber)
    {
        LastPhoneNumber = phoneNumber;
    }
    
    public static string LastPhoneNumber { get; private set; }

    private async Task<string> GenerateFromPhpApiAsync(string phoneNumber)
    {
        try
        {
            var formattedPhone = phoneNumber.StartsWith("+") ? phoneNumber : "+" + phoneNumber;
            var serverKey = _options.CurrentValue.PhpApiServerKey ?? "";
            if (string.IsNullOrWhiteSpace(serverKey))
            {
                return null;
            }
            
            var requestData = new
            {
                phone_number = formattedPhone,
                server_key = serverKey
            };
            var json = JsonConvert.SerializeObject(requestData);
            var content = new StringContent(json, Encoding.UTF8, "application/json");
            var endpoint = new Uri(new Uri(_phpApiBaseUrl), "generate-code");
            var response = await _httpClient.PostAsync(endpoint, content);
            if (response.IsSuccessStatusCode)
            {
                var responseContent = await response.Content.ReadAsStringAsync();
                var result = JsonConvert.DeserializeObject<PhpApiResponse>(responseContent);
                
                if (result?.Success == true && !string.IsNullOrEmpty(result.Code))
                {
                    return result.Code;
                }
            }
            else
            {
                _logger.LogError("PHP API error: {StatusCode} - {ReasonPhrase}", response.StatusCode, response.ReasonPhrase);
            }
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "HTTP request failed to PHP API");
        }
        catch (TaskCanceledException ex)
        {
            _logger.LogError(ex, "Request to PHP API timed out");
        }
        catch (JsonException ex)
        {
            _logger.LogError(ex, "Failed to parse JSON response from PHP API");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error calling PHP API");
        }

        return null;
    }

    private string GenerateDefault()
    {
        try
        {
            var fixedCode = _options.CurrentValue.FixedVerifyCode;
            
            if (!string.IsNullOrWhiteSpace(fixedCode) && fixedCode.All(char.IsDigit) && fixedCode.Length == 5)
            {
                return fixedCode;
            }
            
            return _randomHelper.GenerateRandomNumber(5);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in default code generation");
            return "22222";
        }
    }

    private class PhpApiResponse
    {
        public bool Success { get; set; }
        public string? Code { get; set; }
        public string? Message { get; set; }
    }
}
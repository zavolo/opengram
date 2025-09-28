using System.Net.Http;
using System.Text;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;

namespace MyTelegram.SmsSender;

public class WebSmsSender : ISmsSender, ITransientDependency
{
    private readonly IOptionsMonitor<WebSmsOptions> _options;
    private readonly ILogger<WebSmsSender> _logger;
    private readonly HttpClient _httpClient;
    private readonly string _phpApiBaseUrl;

    public WebSmsSender(IOptionsMonitor<WebSmsOptions> options, ILogger<WebSmsSender> logger, IHttpClientFactory httpClientFactory)
    {
        _options = options;
        _logger = logger;
        _httpClient = httpClientFactory.CreateClient();
        _httpClient.Timeout = TimeSpan.FromSeconds(10);
        _phpApiBaseUrl = options.CurrentValue.CustomPhpApiUrl ?? "https://opengra.me/api/";
    }

    public bool Enabled => _options.CurrentValue.Enabled;

    public async Task SendAsync(SmsMessage smsMessage)
    {
        if (!_options.CurrentValue.Enabled)
        {
            _logger.LogWarning("WebSmsSender disabled, the code will not be sent. PhoneNumber: {To} Text: {Text}", smsMessage.PhoneNumber, smsMessage.Text);
            return;
        }
        try
        {
            var success = await SendSmsViaPhpApiAsync(smsMessage.PhoneNumber, smsMessage.Text);
            if (!success) _logger.LogWarning("SMS sending failed for phone: {PhoneNumber}", smsMessage.PhoneNumber);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in SMS sending");
            throw;
        }
    }

    private async Task<bool> SendSmsViaPhpApiAsync(string phoneNumber, string text)
    {
        try
        {
            var formattedPhone = phoneNumber.StartsWith("+") ? phoneNumber : "+" + phoneNumber;
            var serverKey = _options.CurrentValue.PhpApiServerKey ?? "";
            var requestData = new { phone_number = formattedPhone, message = text, server_key = serverKey };
            var json = JsonConvert.SerializeObject(requestData);
            var content = new StringContent(json, Encoding.UTF8, "application/json");
            var endpoint = new Uri(new Uri(_phpApiBaseUrl), "send-sms");
            var response = await _httpClient.PostAsync(endpoint, content);
            if (response.IsSuccessStatusCode)
            {
                var responseContent = await response.Content.ReadAsStringAsync();
                var result = JsonConvert.DeserializeObject<PhpApiResponse>(responseContent);
                return result?.Success == true;
            }
            else _logger.LogError("PHP API error: {StatusCode} - {ReasonPhrase}", response.StatusCode, response.ReasonPhrase);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in PHP API request");
        }
        return false;
    }

    private class PhpApiResponse
    {
        public bool Success { get; set; }
        public string? Message { get; set; }
    }
}
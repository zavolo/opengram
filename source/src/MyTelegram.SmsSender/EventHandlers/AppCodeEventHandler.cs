using Microsoft.Extensions.Options;
namespace MyTelegram.SmsSender.EventHandlers;

public class AppCodeEventHandler(
    ISmsSenderFactory smsSenderFactory,
    ILogger<AppCodeEventHandler> logger,
    IOptionsMonitor<WebSmsOptions> options)
    : IEventHandler<AppCodeCreatedIntegrationEvent>, ITransientDependency
{
    public async Task HandleEventAsync(AppCodeCreatedIntegrationEvent eventData)
    {
        var phoneNumber = eventData.PhoneNumber;
        if (!phoneNumber.StartsWith("+"))
            phoneNumber = $"+{phoneNumber}";

        var now = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
        if (eventData.Expire < now)
        {
            logger.LogWarning("App code expired, data={@Data}", eventData);
            return;
        }

        try
        {
            var smsSender = smsSenderFactory.Create(eventData.PhoneNumber);
            var brand = options.CurrentValue.Brand ?? "Opengram";
            await smsSender.SendAsync(phoneNumber, $"{brand} code: {eventData.Code}");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Send sms failed, data={@Data}", eventData);
        }
    }
}
namespace MyTelegram.Messenger.Services.Interfaces;

public interface IPhoneBlockingService
{
    Task<bool> IsPhoneBlockedAsync(string phoneNumber);
}
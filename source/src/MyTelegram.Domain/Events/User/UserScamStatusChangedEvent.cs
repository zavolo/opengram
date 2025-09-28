namespace MyTelegram.Domain.Events.User;

public class UserScamStatusChangedEvent(long userId, string phoneNumber, bool scam)
    : AggregateEvent<UserAggregate, UserId>
{
    public long UserId { get; } = userId;
    public string PhoneNumber { get; } = phoneNumber;
    public bool Scam { get; } = scam;
}
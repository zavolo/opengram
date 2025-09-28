namespace MyTelegram.Domain.Events.User;

public class PrivacyRulesChangedEvent : RequestAggregateEvent2<UserAggregate, UserId>
{
    public PrivacyRulesChangedEvent(RequestInfo requestInfo, long userId, IInputPrivacyKey key, IReadOnlyList<IInputPrivacyRule> rules) : base(requestInfo)
    {
        UserId = userId;
        Key = key;
        Rules = rules;
    }

    public long UserId { get; }
    public IInputPrivacyKey Key { get; }
    public IReadOnlyList<IInputPrivacyRule> Rules { get; }
}
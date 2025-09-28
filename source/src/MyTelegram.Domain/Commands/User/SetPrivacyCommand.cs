namespace MyTelegram.Domain.Commands.User;

public class SetPrivacyCommand(
    UserId aggregateId,
    RequestInfo requestInfo,
    IInputPrivacyKey key,
    IReadOnlyList<IInputPrivacyRule> rules)
    : RequestCommand2<UserAggregate, UserId, IExecutionResult>(aggregateId, requestInfo)
{
    public IInputPrivacyKey Key { get; } = key;
    public IReadOnlyList<IInputPrivacyRule> Rules { get; } = rules;
}
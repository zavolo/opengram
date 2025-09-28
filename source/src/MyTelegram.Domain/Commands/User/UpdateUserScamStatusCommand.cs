namespace MyTelegram.Domain.Commands.User;

public class UpdateUserScamStatusCommand(UserId aggregateId, bool scam)
    : Command<UserAggregate, UserId, IExecutionResult>(aggregateId)
{
    public bool Scam { get; } = scam;
}
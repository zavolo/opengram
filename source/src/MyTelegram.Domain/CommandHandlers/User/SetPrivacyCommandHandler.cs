using EventFlow.Commands;
using MyTelegram.Domain.Aggregates.User;
using MyTelegram.Domain.Commands.User;

namespace MyTelegram.Domain.CommandHandlers.User;

public class SetPrivacyCommandHandler : CommandHandler<UserAggregate, UserId, IExecutionResult, SetPrivacyCommand>
{
    public override Task<IExecutionResult> ExecuteCommandAsync(
        UserAggregate aggregate,
        SetPrivacyCommand command,
        CancellationToken cancellationToken)
    {
        aggregate.SetPrivacy(command.RequestInfo, command.Key, command.Rules);
        return Task.FromResult(ExecutionResult.Success());
    }
}
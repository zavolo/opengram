namespace MyTelegram.Domain.CommandHandlers.User;

public class UpdateUserScamStatusCommandHandler : CommandHandler<UserAggregate, UserId, UpdateUserScamStatusCommand>
{
    public override Task ExecuteAsync(UserAggregate aggregate, UpdateUserScamStatusCommand command, CancellationToken cancellationToken)
    {
        aggregate.UpdateUserScamStatus(command.Scam);
        return Task.CompletedTask;
    }
}
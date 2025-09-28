namespace MyTelegram.DataSeeder.DataSeeders;

public class UserDataSeeder(
    ICommandBus commandBus,
    IEventStore eventStore,
    ILogger<UserDataSeeder> logger,
    IOptionsMonitor<MyTelegramDataSeederOptions> options,
    ISnapshotStore snapshotStore)
    : IUserDataSeeder, ITransientDependency
{
    public async Task SeedAsync()
    {
        await CreateOfficialUserAsync();
        await CreateDefaultSupportUserAsync();
        //await CreateBotFatherUserAsync();
        //await CreateGroupAnonymousBotUserAsync();
        await CreateAnonymousUserAsync();

        if (options.CurrentValue.CreateTestUsers)
        {
            var initUserId = MyTelegramConsts.UserIdInitId;
            var testUserCount = 30;
            for (var i = 1; i < testUserCount; i++)
            {
                await CreateUserIfNeedAsync(initUserId + i,
                    $"1{i}",
                    $"{i}",
                    $"{i}",
                    $"user{i}",
                    false);
            }
        }
    }

    public async Task<bool> CreateUserIfNeedAsync(long userId,
        string phoneNumber,
        string firstName,
        string? lastName,
        string? userName,
        bool bot)
    {
        var aggregateId = UserId.Create(userId);
        var u = new UserAggregate(aggregateId);
        await u.LoadAsync(eventStore, snapshotStore, default);
        if (u.IsNew)
        {
            var accessHash = Random.Shared.NextInt64();
            var createUserCommand =
                new CreateUserCommand(aggregateId,
                    RequestInfo.Empty with { Date = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() },
                    userId,
                    accessHash,
                    phoneNumber,
                    firstName,
                    lastName,
                    userName,
                    bot
                );
            await commandBus.PublishAsync(createUserCommand);

            if (userId != MyTelegramConsts.OfficialUserId)
            {
                var command = new UpdateUserPremiumStatusCommand(u.Id, true);
                await commandBus.PublishAsync(command);
            }

            logger.LogInformation("User {UserName} created successfully", userName ?? firstName);

            return true;
        }

        return false;
    }

    private async Task CreateDefaultSupportUserAsync()
    {
        var userId = MyTelegramConsts.DefaultSupportUserId;
        var created = await CreateUserIfNeedAsync(userId,
            MyTelegramConsts.DefaultSupportUserId.ToString(),
            $"{options.CurrentValue.Brand} Support",
            null,
            null,
            false);

        if (created)
        {
            var command = new SetSupportCommand(UserId.Create(userId), true);
            await commandBus.PublishAsync(command);

            var setVerifiedCommand = new SetVerifiedCommand(UserId.Create(userId), true);
            await commandBus.PublishAsync(setVerifiedCommand);
            logger.LogInformation($"{options.CurrentValue.Brand} support user created successfully");
        }
    }

    private async Task CreateAnonymousUserAsync()
    {
        var userId = MyTelegramConsts.AnonymousUserId;
        var firstName = "Anonymous User";
        await CreateUserIfNeedAsync(userId, string.Empty, firstName, null, null, false);
    }

    private async Task CreateOfficialUserAsync()
    {
        var userId = MyTelegramConsts.OfficialUserId;
        var created = await CreateUserIfNeedAsync(userId,
            "42777",
            options.CurrentValue.Brand,
            null,
            null,
            false);

        if (created)
        {
            var command = new SetSupportCommand(UserId.Create(userId), true);
            await commandBus.PublishAsync(command);

            var setVerifiedCommand = new SetVerifiedCommand(UserId.Create(userId), true);
            await commandBus.PublishAsync(setVerifiedCommand);
            logger.LogInformation($"{options.CurrentValue.Brand} notification user created successfully");
        }
    }
}
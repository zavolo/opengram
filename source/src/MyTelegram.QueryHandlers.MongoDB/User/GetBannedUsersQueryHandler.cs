namespace MyTelegram.QueryHandlers.MongoDB.User;

public class GetBannedUsersQueryHandler(IQueryOnlyReadModelStore<UserReadModel> store) : IQueryHandler<GetBannedUsersQuery, IReadOnlyCollection<IUserReadModel>>
{
    public async Task<IReadOnlyCollection<IUserReadModel>> ExecuteQueryAsync(GetBannedUsersQuery query, CancellationToken cancellationToken)
    {
        return await store.FindAsync(p => p.IsBan == true, cancellationToken: cancellationToken);
    }
}
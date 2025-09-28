// ReSharper disable All
using MyTelegram.Messenger.Services.Interfaces;

namespace MyTelegram.Messenger.Handlers.LatestLayer.Impl.Account;

///<summary>
/// Get privacy settings of current account
/// <para>Possible errors</para>
/// Code Type Description
/// 400 PRIVACY_KEY_INVALID The privacy key is invalid.
/// See <a href="https://corefork.telegram.org/method/account.getPrivacy" />
///</summary>
internal sealed class GetPrivacyHandler(
    IPrivacyAppService privacyAppService,
    IUserAppService userAppService) 
    : RpcResultObjectHandler<MyTelegram.Schema.Account.RequestGetPrivacy, MyTelegram.Schema.Account.IPrivacyRules>,
    Account.IGetPrivacyHandler
{
    protected override async Task<MyTelegram.Schema.Account.IPrivacyRules> HandleCoreAsync(IRequestInput input,
        MyTelegram.Schema.Account.RequestGetPrivacy obj)
    {
        try
        {
            var privacyRules = await privacyAppService.GetPrivacyRulesAsync(
                input.UserId,
                obj.Key
            );
            var userIds = ExtractUserIds(privacyRules);            
            var users = new TVector<IUser>();
            if (userIds.Count > 0)
            {
                var userReadModels = await userAppService.GetListAsync(userIds);
                var userList = userReadModels
                    .Where(u => u != null && u.IsDeleted != true)
                    .Select(ConvertUserReadModelToUser)
                    .ToList();
                users.AddRange(userList);
            }
            var rules = new TVector<IPrivacyRule>();
            rules.AddRange(privacyRules);
            return new TPrivacyRules
            {
                Rules = rules,
                Users = users,
                Chats = new TVector<IChat>()
            };
        }
        catch (ArgumentException ex) when (ex.Message.Contains("privacy key"))
        {
            throw new RpcException(RpcErrors.RpcErrors400.PrivacyKeyInvalid);
        }
    }
    
    private List<long> ExtractUserIds(IReadOnlyList<IPrivacyRule> rules)
    {
        var userIds = new List<long>();
        foreach (var rule in rules)
        {
            switch (rule)
            {
                case TPrivacyValueAllowUsers allowUsers:
                    userIds.AddRange(allowUsers.Users);
                    break;
                
                case TPrivacyValueDisallowUsers disallowUsers:
                    userIds.AddRange(disallowUsers.Users);
                    break;
            }
        }
        return userIds.Distinct().ToList();
    }
    
    private IUser ConvertUserReadModelToUser(IUserReadModel userReadModel)
    {
        return new TUser
        {
            Id = userReadModel.UserId,
            AccessHash = userReadModel.AccessHash,
            FirstName = userReadModel.FirstName,
            LastName = userReadModel.LastName,
            Username = userReadModel.UserName,
            Phone = userReadModel.PhoneNumber,
            Bot = userReadModel.Bot,
            Verified = userReadModel.Verified,
            Premium = userReadModel.Premium,
            Support = userReadModel.Support
        };
    }
}
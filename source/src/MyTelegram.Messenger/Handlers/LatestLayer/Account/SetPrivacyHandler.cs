// ReSharper disable All
using MyTelegram.Messenger.Services.Interfaces;

namespace MyTelegram.Messenger.Handlers.LatestLayer.Impl.Account;

///<summary>
/// Change privacy settings of current account
/// <para>Possible errors</para>
/// Code Type Description
/// 400 PRIVACY_KEY_INVALID The privacy key is invalid.
/// 400 PRIVACY_TOO_LONG Too many privacy rules were specified, the current limit is 1000.
/// 400 PRIVACY_VALUE_INVALID The specified privacy rule combination is invalid.
/// See <a href="https://corefork.telegram.org/method/account.setPrivacy" />
///</summary>
internal sealed class SetPrivacyHandler(IPrivacyAppService privacyAppService, IUserAppService userAppService) 
    : RpcResultObjectHandler<MyTelegram.Schema.Account.RequestSetPrivacy, MyTelegram.Schema.Account.IPrivacyRules>,
      Account.ISetPrivacyHandler
{
    protected override async Task<MyTelegram.Schema.Account.IPrivacyRules> HandleCoreAsync(IRequestInput input,
        MyTelegram.Schema.Account.RequestSetPrivacy obj)
    {
        if (obj.Rules?.Count > 1000)
        {
            throw new RpcException(RpcErrors.RpcErrors400.PrivacyTooLong);
        }

        if (obj.Rules == null || obj.Rules.Count == 0)
        {
            throw new RpcException(RpcErrors.RpcErrors400.PrivacyValueInvalid);
        }

        try
        {
            var setPrivacyOutput = await privacyAppService.SetPrivacyAsync(
                input.ToRequestInfo(),
                input.UserId,
                obj.Key,
                obj.Rules.ToList()
            );
            var userIds = ExtractUserIds(obj.Rules.ToList(), input);
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
            rules.AddRange(setPrivacyOutput.Rules);
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
        catch (ArgumentException)
        {
            throw new RpcException(RpcErrors.RpcErrors400.PrivacyValueInvalid);
        }
    }

    private List<long> ExtractUserIds(List<IInputPrivacyRule> rules, IRequestInput input)
    {
        var userIds = new List<long>();
        foreach (var rule in rules)
        {
            switch (rule)
            {
                case TInputPrivacyValueAllowUsers allowUsers:
                    userIds.AddRange(allowUsers.Users.Select(u => GetUserIdFromInputUser(u, input)));
                    break;
                
                case TInputPrivacyValueDisallowUsers disallowUsers:
                    userIds.AddRange(disallowUsers.Users.Select(u => GetUserIdFromInputUser(u, input)));
                    break;
            }
        }
        return userIds.Distinct().ToList();
    }

    private long GetUserIdFromInputUser(IInputUser inputUser, IRequestInput input)
    {
        return inputUser switch
        {
            TInputUser user => user.UserId,
            TInputUserSelf => input.UserId,
            _ => 0
        };
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
using MyTelegram.Domain.Commands.User;
using MyTelegram.ReadModel.Impl;

namespace MyTelegram.Messenger.Services.Impl;

public class PrivacyAppService(
    ICacheManager<GlobalPrivacySettingsCacheItem> cacheManager,
    IQueryProcessor queryProcessor,
    ICommandBus commandBus)
    : BaseAppService, IPrivacyAppService, ITransientDependency
{
    public Task<IReadOnlyCollection<IPrivacyReadModel>> GetPrivacyListAsync(IReadOnlyList<long> userIds)
    {
        return Task.FromResult<IReadOnlyCollection<IPrivacyReadModel>>([]);
    }

    public Task<IReadOnlyCollection<IPrivacyReadModel>> GetPrivacyListAsync(long userId)
    {
        return GetPrivacyListAsync([userId]);
    }

    public Task ApplyPrivacyAsync(long selfUserId, long targetUserId, Action executeOnPrivacyNotMatch, List<PrivacyType> privacyTypes)
    {
        return Task.CompletedTask;
    }

    public Task ApplyPrivacyListAsync(long selfUserId, IReadOnlyList<long> targetUserIdList, Action<long> executeOnPrivacyNotMatch,
        List<PrivacyType> privacyTypes)
    {
        return Task.CompletedTask;
    }

    public async Task<IReadOnlyList<IPrivacyRule>> GetPrivacyRulesAsync(long selfUserId, IInputPrivacyKey key)
    {
        var userReadModel = await queryProcessor.ProcessAsync(new GetUserByIdQuery(selfUserId), default);
        var keyName = key.GetType().Name;

        if (userReadModel?.PrivacyRules != null && userReadModel.PrivacyRules.TryGetValue(keyName, out var rules))
        {
            return rules;
        }

        return Array.Empty<IPrivacyRule>();
    }

    public Task ApplyPrivacyListAsync(long selfUserId, IReadOnlyList<long> targetUserIdList, Action<PrivacyValueType, long> executeOnPrivacyNotMatch,
        List<PrivacyType> privacyTypes)
    {
        return Task.CompletedTask;
    }

    public Task SetGlobalPrivacySettingsAsync(long selfUserId, GlobalPrivacySettings globalPrivacySettings)
    {
        return Task.CompletedTask;
    }

    public async Task<GlobalPrivacySettingsCacheItem?> GetGlobalPrivacySettingsAsync(long userId)
    {
        var cacheKey = GlobalPrivacySettingsCacheItem.GetCacheKey(userId);
        var item = await cacheManager.GetAsync(cacheKey);
        var globalPrivacySettings = await queryProcessor.ProcessAsync(new GetGlobalPrivacySettingsQuery(userId));
        if (globalPrivacySettings != null)
        {
            item = new GlobalPrivacySettingsCacheItem(globalPrivacySettings.ArchiveAndMuteNewNoncontactPeers,
                globalPrivacySettings.KeepArchivedUnmuted, globalPrivacySettings.KeepArchivedFolders,
                globalPrivacySettings.HideReadMarks, globalPrivacySettings.NewNoncontactPeersRequirePremium);
            await cacheManager.SetAsync(cacheKey, item);
        }
        return item;
    }

    public PrivacyValueData GetPrivacyValueData(IInputPrivacyRule rule)
    {
        throw new NotImplementedException();
    }

    public List<PrivacyValueData> GetPrivacyValueDataList(IList<IInputPrivacyRule> rules)
    {
        return [];
    }

    public async Task<SetPrivacyOutput> SetPrivacyAsync(RequestInfo requestInfo,
                                        long selfUserId,
                                        IInputPrivacyKey key,
                                        IReadOnlyList<IInputPrivacyRule> ruleList)
    {
        var command = new SetPrivacyCommand(UserId.Create(selfUserId), requestInfo, key, ruleList);
        await commandBus.PublishAsync(command, CancellationToken.None);

        var convertedRules = new List<IPrivacyRule>();
        foreach (var rule in ruleList)
        {
            switch (rule)
            {
                case TInputPrivacyValueAllowAll:
                    convertedRules.Add(new TPrivacyValueAllowAll());
                    break;
                case TInputPrivacyValueAllowContacts:
                    convertedRules.Add(new TPrivacyValueAllowContacts());
                    break;
                case TInputPrivacyValueAllowUsers allowUsers:
                    var allowedUserIds = allowUsers.Users.Select(u => u switch { TInputUserSelf => selfUserId, TInputUser user => user.UserId, _ => 0L }).ToList();
                    convertedRules.Add(new TPrivacyValueAllowUsers { Users = new TVector<long>(allowedUserIds) });
                    break;
                case TInputPrivacyValueDisallowAll:
                    convertedRules.Add(new TPrivacyValueDisallowAll());
                    break;
                case TInputPrivacyValueDisallowContacts:
                    convertedRules.Add(new TPrivacyValueDisallowContacts());
                    break;
                case TInputPrivacyValueDisallowUsers disallowUsers:
                    var disallowedUserIds = disallowUsers.Users.Select(u => u switch { TInputUserSelf => selfUserId, TInputUser user => user.UserId, _ => 0L }).ToList();
                    convertedRules.Add(new TPrivacyValueDisallowUsers { Users = new TVector<long>(disallowedUserIds) });
                    break;
            }
        }
        return new SetPrivacyOutput(convertedRules);
    }

    public Task ApplyPrivacyAsync(long selfUserId, long targetUserId, Action<PrivacyValueType> executeOnPrivacyNotMatch, PrivacyType privacyType)
    {
        return Task.CompletedTask;
    }

    public Task ApplyPrivacyAsync(long selfUserId, long targetUserId, Action<PrivacyValueType> executeOnPrivacyNotMatch, List<PrivacyType> privacyTypes)
    {
        return Task.CompletedTask;
    }
}
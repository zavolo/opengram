// ReSharper disable All
using MyTelegram.Schema.Fragment;
using MyTelegram.Messenger.Services.Interfaces;

namespace MyTelegram.Messenger.Handlers.LatestLayer.Fragment;

///<summary>
/// Fetch information about a <a href="https://corefork.telegram.org/api/fragment#fetching-info-about-fragment-collectibles">fragment collectible, see here »</a> for more info on the full flow.
/// <para>Possible errors</para>
/// Code Type Description
/// 400 COLLECTIBLE_INVALID The specified collectible is invalid.
/// 400 COLLECTIBLE_NOT_FOUND The specified collectible could not be found.
/// See <a href="https://corefork.telegram.org/method/fragment.getCollectibleInfo" />
///</summary>
internal sealed class GetCollectibleInfoHandler(
    IUserAppService userAppService,
    IQueryProcessor queryProcessor,
    ILogger<GetCollectibleInfoHandler> logger) 
    : RpcResultObjectHandler<MyTelegram.Schema.Fragment.RequestGetCollectibleInfo, MyTelegram.Schema.Fragment.ICollectibleInfo>
{
    private const int MaxUsernameLength = 32;
    
    protected override async Task<MyTelegram.Schema.Fragment.ICollectibleInfo> HandleCoreAsync(IRequestInput input,
        MyTelegram.Schema.Fragment.RequestGetCollectibleInfo obj)
    {
        try
        {
            ValidateCollectibleRequest(obj);
            
            var user = await userAppService.GetAsync(input.UserId);
            if (user == null)
            {
                RpcErrors.RpcErrors400.CollectibleNotFound.ThrowRpcError();
            }

            string collectibleUsername = await GetCollectibleUsernameAsync(input.UserId, obj.Collectible);
            
            if (string.IsNullOrEmpty(collectibleUsername))
            {
                RpcErrors.RpcErrors400.CollectibleNotFound.ThrowRpcError();
            }

            return new TCollectibleInfo
            {
                PurchaseDate = await GetCollectiblePurchaseDateAsync(collectibleUsername, input.UserId),
                Currency = "USD",
                Amount = CalculateCollectibleAmount(collectibleUsername),
                CryptoCurrency = "TON",
                CryptoAmount = CalculateCryptoAmount(collectibleUsername),
                Url = $"https://opengra.me/username/{collectibleUsername}"
            };
        }
        catch (Exception ex) when (!(ex is RpcException))
        {
            logger.LogError(ex, "Error getting collectible info for user {UserId}", input.UserId);
            RpcErrors.RpcErrors400.CollectibleInvalid.ThrowRpcError();
            throw;
        }
    }
    
    private void ValidateCollectibleRequest(RequestGetCollectibleInfo obj)
    {
        if (obj.Collectible == null)
        {
            RpcErrors.RpcErrors400.CollectibleInvalid.ThrowRpcError();
        }
        
        if (obj.Collectible is TInputCollectibleUsername usernameCollectible)
        {
            if (string.IsNullOrWhiteSpace(usernameCollectible.Username) || 
                usernameCollectible.Username.Length > MaxUsernameLength)
            {
                RpcErrors.RpcErrors400.CollectibleInvalid.ThrowRpcError();
            }
        }
    }
    
    private async Task<string> GetCollectibleUsernameAsync(long userId, IInputCollectible collectible)
    {
        try
        {
            if (collectible is TInputCollectibleUsername usernameCollectible)
            {
                var collectibleInfo = await queryProcessor.ProcessAsync(
                    new GetCollectibleUsernameByUsernameQuery(usernameCollectible.Username));
                    
                if (collectibleInfo == null || collectibleInfo.OwnerPeerId != userId)
                {
                    return string.Empty;
                }
                    
                return collectibleInfo.UserName;
            }
            
            return string.Empty;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error getting collectible username for user {UserId}", userId);
            return string.Empty;
        }
    }
    
    private async Task<int> GetCollectiblePurchaseDateAsync(string username, long userId)
    {
        try
        {
            var userNameReadModel = await queryProcessor.ProcessAsync(
                new GetUserNameByNameQuery(username));
                
            if (userNameReadModel != null)
            {
                return userNameReadModel.Date;
            }
            
            var user = await userAppService.GetAsync(userId);
            return user?.CreationTime?.ToTimestamp() ?? CurrentDate;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error getting purchase date for username {Username}", username);
            return CurrentDate;
        }
    }
    
    private long CalculateCollectibleAmount(string username)
    {
        if (string.IsNullOrEmpty(username))
            return 0;
            
        long baseAmount = 50000000000;
        
        if (username.Length <= 3)
            return baseAmount * 10;
        if (username.Length <= 5)
            return baseAmount * 3;
            
        return baseAmount;
    }
    
    private long CalculateCryptoAmount(string username)
    {
        var usdAmount = CalculateCollectibleAmount(username);
        var tonRate = 2500000000;
        return usdAmount / tonRate * 1000000000;
    }
}
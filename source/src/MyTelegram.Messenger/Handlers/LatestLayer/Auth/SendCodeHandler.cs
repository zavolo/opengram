using MyTelegram.Messenger.Services.Impl;
using MyTelegram.Messenger.Services.Interfaces;
namespace MyTelegram.Messenger.Handlers.LatestLayer.Impl.Auth;

///<summary>
/// Send the verification code for login
/// <para>Possible errors</para>
/// Code Type Description
/// 400 API_ID_INVALID API ID invalid.
/// 400 API_ID_PUBLISHED_FLOOD This API id was published somewhere, you can't use it now.
/// 500 AUTH_RESTART Restart the authorization process.
/// 400 PHONE_NUMBER_APP_SIGNUP_FORBIDDEN You can't sign up using this app.
/// 400 PHONE_NUMBER_BANNED The provided phone number is banned from telegram.
/// 400 PHONE_NUMBER_FLOOD You asked for the code too many times.
/// 406 PHONE_NUMBER_INVALID The phone number is invalid.
/// 406 PHONE_PASSWORD_FLOOD You have tried logging in too many times.
/// 400 PHONE_PASSWORD_PROTECTED This phone is password protected.
/// 400 SMS_CODE_CREATE_FAILED An error occurred while creating the SMS code.
/// 406 UPDATE_APP_TO_LOGIN Please update your client to login.
/// See <a href="https://corefork.telegram.org/method/auth.sendCode" />
///</summary>
internal sealed class SendCodeHandler(
    ICommandBus commandBus,
    IPeerHelper peerHelper,
    IOptionsMonitor<MyTelegramMessengerServerOptions> options,
    IQueryProcessor queryProcessor,
    ICacheManager<FutureAuthTokenCacheItem> cacheManager,
    IHashHelper hashHelper,
    ICountryHelper countryHelper,
    ICacheHelper<long, long> cacheHelper,
    IScheduleAppService scheduleAppService,
    ILayeredService<IAuthorizationConverter> authorizationLayeredService,
    IUserConverterService userConverterService,
    IVerificationCodeGenerator verificationCodeGenerator,
    IEventBus eventBus,
    IPhoneBlockingService phoneBlockingService,
    ILogger<SendCodeHandler> logger)
    : RpcResultObjectHandler<Schema.Auth.RequestSendCode, Schema.Auth.ISentCode>,
        Auth.ISendCodeHandler
{
    private readonly int _maxFutureAuthTokens = 20;
    private readonly int _maxPhoneNumberLength = 15;
    private readonly int _maxLogoutTokens = 100;
    
    protected override async Task<ISentCode> HandleCoreAsync(IRequestInput input,
        RequestSendCode obj)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(obj.PhoneNumber) || obj.PhoneNumber.Length > _maxPhoneNumberLength)
            {
                RpcErrors.RpcErrors400.PhoneNumberInvalid.ThrowRpcError();
            }

            var phoneNumber = obj.PhoneNumber.ToPhoneNumber();
            
            if (await phoneBlockingService.IsPhoneBlockedAsync(obj.PhoneNumber))
            {
                RpcErrors.RpcErrors400.PhoneNumberBanned.ThrowRpcError();
            }
            
            CheckPhoneNumber(phoneNumber);

            var userReadModel = 
                await queryProcessor.ProcessAsync(new GetUserByPhoneNumberQuery(phoneNumber));
                
            if (userReadModel != null)
            {
                if (peerHelper.IsBotUser(userReadModel.UserId) || 
                    userReadModel.UserId == MyTelegramConsts.OfficialUserId)
                {
                    RpcErrors.RpcErrors400.PhoneNumberInvalid.ThrowRpcError();
                }

                (var sentCode, bool loginSuccess) = await SignInWithFutureAuthTokenAsync(input, obj, userReadModel);
                if (loginSuccess)
                {
                    return sentCode!;
                }
            }

            string code = GenerateVerificationCode(phoneNumber);
            if (string.IsNullOrEmpty(code))
            {
                RpcErrors.RpcErrors400.SmsCodeCreateFailed.ThrowRpcError();
            }
            
            var phoneCodeHash = Guid.NewGuid().ToString("N");
            var timeout = Math.Max(60, Math.Min(3600, options.CurrentValue.VerificationCodeExpirationSeconds));
            
            var result = await SendSmsCodeAsync(userReadModel, input, obj.PhoneNumber, phoneCodeHash, timeout, code);
            
            return result;
        }
        catch (Exception ex) when (!(ex is RpcException))
        {
            logger.LogError(ex, "Unexpected error in SendCode for phone: {PhoneNumber}", obj.PhoneNumber);
            RpcErrors.RpcErrors500.InternalError.ThrowRpcError();
            throw;
        }
    }
    
    private string GenerateVerificationCode(string phoneNumber)
    {
        try
        {
            if (verificationCodeGenerator is IVerificationCodeGeneratorEx generatorEx)
            {
                return generatorEx.Generate(phoneNumber);
            }
            else if (verificationCodeGenerator is CustomVerificationCodeGenerator customGenerator)
            {
                return customGenerator.Generate(phoneNumber);
            }
            else
            {
                return verificationCodeGenerator.Generate();
            }
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error generating verification code for phone: {PhoneNumber}", phoneNumber);
            return null;
        }
    }
    

    
    private void CheckPhoneNumber(string phoneNumber)
    {
        if (string.IsNullOrWhiteSpace(phoneNumber))
        {
            RpcErrors.RpcErrors400.PhoneNumberInvalid.ThrowRpcError();
        }

        if (!long.TryParse(phoneNumber, out var phoneNumberLong) || phoneNumberLong <= 0)
        {
            RpcErrors.RpcErrors400.PhoneNumberInvalid.ThrowRpcError();
        }

        if (options.CurrentValue.CheckPhoneNumberFormat)
        {
            if (phoneNumber.Length < 5 || phoneNumber.Length > _maxPhoneNumberLength)
            {
                RpcErrors.RpcErrors400.PhoneNumberInvalid.ThrowRpcError();
            }

            var phoneNumberWithoutCountryCode = phoneNumber;
            CountryCodeItem? countryCodeItem = null;

            var maxCountryCodeLength = Math.Min(4, phoneNumber.Length - 1);
            for (int i = 1; i <= maxCountryCodeLength; i++)
            {
                if (countryHelper.TryGetCountryCodeItem(phoneNumber[..i], out countryCodeItem))
                {
                    phoneNumberWithoutCountryCode = phoneNumber[i..];
                    break;
                }
            }

            if (countryCodeItem?.PhoneNumberLengths?.Count > 0)
            {
                var phoneNumberLength = phoneNumberWithoutCountryCode.Length;
                var isValidPhoneNumber = countryCodeItem.PhoneNumberLengths.Any(p => p == phoneNumberLength);
                if (!isValidPhoneNumber)
                {
                    RpcErrors.RpcErrors400.PhoneNumberInvalid.ThrowRpcError();
                }
            }
        }
    }

    private async Task<ISentCode> SendSmsCodeAsync(IUserReadModel? userReadModel, IRequestInput input,
        string phoneNumber, string phoneCodeHash, int timeout, string code)
    {
        try
        {
            var appCodeId = AppCodeId.Create(phoneNumber.ToPhoneNumber(), phoneCodeHash);
            
            var sendAppCodeCommand =
                new SendAppCodeCommand(appCodeId,
                    input.ToRequestInfo() with { UserId = userReadModel?.UserId ?? 0 },
                    userReadModel?.UserId ?? 0,
                    phoneNumber.ToPhoneNumber(),
                    code,
                    phoneCodeHash,
                    DateTime.UtcNow.ToTimestamp());
                    
            await commandBus.PublishAsync(sendAppCodeCommand);

            return new TSentCode
            {
                Type = new TSentCodeTypeSms { Length = Math.Min(10, Math.Max(4, code.Length)) },
                PhoneCodeHash = phoneCodeHash,
                Timeout = timeout
            };
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error sending SMS code for phone: {PhoneNumber}", phoneNumber);
            RpcErrors.RpcErrors400.SmsCodeCreateFailed.ThrowRpcError();
            throw;
        }
    }

    private async Task<(ISentCode? sentCode, bool loginSuccess)> SignInWithFutureAuthTokenAsync(IRequestInput input,
        RequestSendCode obj, IUserReadModel userReadModel)
    {
        try
        {
            if (obj.Settings?.LogoutTokens?.Count > 0)
            {
                var logoutTokensCount = Math.Min(obj.Settings.LogoutTokens.Count, _maxLogoutTokens);
                var limitedLogoutTokens = obj.Settings.LogoutTokens.Take(logoutTokensCount);
                
                var cacheKeys = limitedLogoutTokens.Take(_maxFutureAuthTokens)
                    .Where(token => token != null && token.Length > 0)
                    .Select(p => FutureAuthTokenCacheItem.GetCacheKey(
                        BitConverter.ToString(hashHelper.Sha1(p)).Replace("-", string.Empty)))
                    .ToList();

                if (cacheKeys.Count > 0)
                {
                    var cachedFutureTokens = await cacheManager.GetManyAsync(cacheKeys);

                    var validToken = cachedFutureTokens.FirstOrDefault(p => 
                        p.Value != null && p.Value.UserId == userReadModel.UserId);

                    if (validToken.Value != null)
                    {
                        if (userReadModel.HasPassword)
                        {
                            await eventBus.PublishAsync(new UserSignInSuccessEvent(
                                input.ReqMsgId,
                                input.AuthKeyId,
                                input.PermAuthKeyId,
                                userReadModel.UserId,
                                PasswordState.WaitingForVerify,
                                true
                            ));

                            return (null, true);
                        }
                        else
                        {
                            var user = userConverterService.ToUser(input, userReadModel, null, layer: input.Layer);

                            await eventBus.PublishAsync(new UserSignInSuccessEvent(
                                input.ReqMsgId,
                                input.AuthKeyId, 
                                input.PermAuthKeyId,
                                user.Id, 
                                PasswordState.None));

                            return (new TSentCodeSuccess
                            {
                                Authorization = authorizationLayeredService.GetConverter(input.Layer)
                                    .CreateAuthorization(user)
                            }, true);
                        }
                    }
                }
            }
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error during future auth token validation for user: {UserId}", userReadModel.UserId);
        }

        return (null, false);
    }
}
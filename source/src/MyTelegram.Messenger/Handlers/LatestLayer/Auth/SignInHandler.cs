namespace MyTelegram.Messenger.Handlers.LatestLayer.Auth;

///<summary>
/// Signs in a user with a validated phone number.
/// <para>Possible errors</para>
/// Code Type Description
/// 500 AUTH_RESTART Restart the authorization process.
/// 400 PHONE_CODE_EMPTY phone_code is missing.
/// 400 PHONE_CODE_EXPIRED The phone code you provided has expired.
/// 400 PHONE_CODE_INVALID The provided phone code is invalid.
/// 406 PHONE_NUMBER_INVALID The phone number is invalid.
/// 400 PHONE_NUMBER_UNOCCUPIED The phone number is not yet being used.
/// 500 SIGN_IN_FAILED Failure while signing in.
/// See <a href="https://corefork.telegram.org/method/auth.signIn" />
///</summary>
internal sealed class SignInHandler(
    ICommandBus commandBus,
    ILogger<SignInHandler> logger,
    IQueryProcessor queryProcessor,
    IPhoneBlockingService phoneBlockingService)
    : RpcResultObjectHandler<MyTelegram.Schema.Auth.RequestSignIn, MyTelegram.Schema.Auth.IAuthorization>
{
    private const int MaxPhoneLength = 15;
    private const int MaxPhoneCodeLength = 10;
    private const int MaxPhoneCodeHashLength = 64;
    
    protected override async Task<MyTelegram.Schema.Auth.IAuthorization> HandleCoreAsync(IRequestInput input,
        RequestSignIn obj)
    {
        try
        {
            ValidateSignInRequest(obj);

            if (await phoneBlockingService.IsPhoneBlockedAsync(obj.PhoneNumber))
            {
                RpcErrors.RpcErrors400.PhoneNumberBanned.ThrowRpcError();
            }

            var phoneNumber = obj.PhoneNumber.ToPhoneNumber();
            var userId = 0L;
            
            var userReadModel = await queryProcessor
                    .ProcessAsync(new GetUserByPhoneNumberQuery(phoneNumber), default);
                    
            if (userReadModel == null)
            {
                RpcErrors.RpcErrors400.PhoneNumberUnoccupied.ThrowRpcError();
            }
            else
            {
                userId = userReadModel.UserId;
            }

            var sanitizedPhoneCode = SanitizePhoneCode(obj.PhoneCode);
            var command = new CheckSignInCodeCommand(
                AppCodeId.Create(phoneNumber, obj.PhoneCodeHash),
                input.ToRequestInfo() with { UserId = userId },
                sanitizedPhoneCode,
                userId
            );

            await commandBus.PublishAsync(command);

            return null!;
        }
        catch (Exception ex) when (!(ex is RpcException))
        {
            logger.LogError(ex, "Unexpected error in SignIn for phone: {PhoneNumber}", obj.PhoneNumber);
            RpcErrors.RpcErrors500.SignInFailed.ThrowRpcError();
            throw;
        }
    }
    
    private void ValidateSignInRequest(RequestSignIn obj)
    {
        if (string.IsNullOrWhiteSpace(obj.PhoneNumber) || obj.PhoneNumber.Length > MaxPhoneLength)
        {
            RpcErrors.RpcErrors400.PhoneNumberInvalid.ThrowRpcError();
        }
        
        if (string.IsNullOrWhiteSpace(obj.PhoneCode) || obj.PhoneCode.Length > MaxPhoneCodeLength)
        {
            RpcErrors.RpcErrors400.PhoneCodeEmpty.ThrowRpcError();
        }
        
        if (string.IsNullOrWhiteSpace(obj.PhoneCodeHash) || obj.PhoneCodeHash.Length > MaxPhoneCodeHashLength)
        {
            RpcErrors.RpcErrors400.PhoneCodeEmpty.ThrowRpcError();
        }
        
        if (!obj.PhoneCode.All(char.IsDigit))
        {
            RpcErrors.RpcErrors400.PhoneCodeInvalid.ThrowRpcError();
        }
        
        if (!long.TryParse(obj.PhoneNumber, out var phoneNumberLong) || phoneNumberLong <= 0)
        {
            RpcErrors.RpcErrors400.PhoneNumberInvalid.ThrowRpcError();
        }
    }
    
    private string SanitizePhoneCode(string phoneCode)
    {
        if (string.IsNullOrEmpty(phoneCode))
            return string.Empty;
            
        var sanitized = new string(phoneCode.Where(char.IsDigit).ToArray());
        return sanitized.Length > MaxPhoneCodeLength ? sanitized.Substring(0, MaxPhoneCodeLength) : sanitized;
    }
}
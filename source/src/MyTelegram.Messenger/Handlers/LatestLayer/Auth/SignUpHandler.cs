using MyTelegram.Messenger.Services.Interfaces;
namespace MyTelegram.Messenger.Handlers.LatestLayer.Impl.Auth;

///<summary>
/// Registers a validated phone number in the system.
/// <para>Possible errors</para>
/// Code Type Description
/// 400 FIRSTNAME_INVALID The first name is invalid.
/// 400 LASTNAME_INVALID The last name is invalid.
/// 400 PHONE_CODE_EMPTY phone_code is missing.
/// 400 PHONE_CODE_EXPIRED The phone code you provided has expired.
/// 400 PHONE_CODE_INVALID The provided phone code is invalid.
/// 400 PHONE_NUMBER_FLOOD You asked for the code too many times.
/// 406 PHONE_NUMBER_INVALID The phone number is invalid.
/// 400 PHONE_NUMBER_OCCUPIED The phone number is already in use.
/// See <a href="https://corefork.telegram.org/method/auth.signUp" />
///</summary>
internal sealed class SignUpHandler(
    ICommandBus commandBus,
    IRandomHelper randomHelper,
    IQueryProcessor queryProcessor,
    IPhoneBlockingService phoneBlockingService,
    ILogger<SignUpHandler> logger)
    : RpcResultObjectHandler<MyTelegram.Schema.Auth.RequestSignUp, MyTelegram.Schema.Auth.IAuthorization>,
        Auth.ISignUpHandler
{
    private const int MaxNameLength = 64;
    private const int MaxPhoneLength = 15;
    
    protected override async Task<MyTelegram.Schema.Auth.IAuthorization> HandleCoreAsync(IRequestInput input,
        RequestSignUp obj)
    {
        try
        {
            ValidateSignUpRequest(obj);
            
            var phoneNumber = obj.PhoneNumber.ToPhoneNumber();
           
            if (await phoneBlockingService.IsPhoneBlockedAsync(obj.PhoneNumber))
            {
                RpcErrors.RpcErrors400.PhoneNumberBanned.ThrowRpcError();
            }
           
            var userReadModel = await queryProcessor
                .ProcessAsync(new GetUserByPhoneNumberQuery(phoneNumber));
            
            if (userReadModel != null)
            {
                RpcErrors.RpcErrors400.PhoneNumberOccupied.ThrowRpcError();
            }
            
            var userId = userReadModel?.UserId ?? 0;
            var command = new CheckSignUpCodeCommand(
                AppCodeId.Create(phoneNumber, obj.PhoneCodeHash),
                input.ToRequestInfo(),
                obj.PhoneCodeHash,
                userId,
                randomHelper.NextInt64(),
                phoneNumber,
                SanitizeString(obj.FirstName, MaxNameLength),
                SanitizeString(obj.LastName ?? string.Empty, MaxNameLength)
            );
            
            await commandBus.PublishAsync(command);
            return null!;
        }
        catch (Exception ex) when (!(ex is RpcException))
        {
            logger.LogError(ex, "Unexpected error in SignUp for phone: {PhoneNumber}", obj.PhoneNumber);
            RpcErrors.RpcErrors500.InternalError.ThrowRpcError();
            throw;
        }
    }
    
    private void ValidateSignUpRequest(RequestSignUp obj)
    {
        if (string.IsNullOrWhiteSpace(obj.PhoneCodeHash) || obj.PhoneCodeHash.Length > 64)
        {
            RpcErrors.RpcErrors400.PhoneCodeEmpty.ThrowRpcError();
        }
        
        if (string.IsNullOrWhiteSpace(obj.PhoneNumber) || obj.PhoneNumber.Length > MaxPhoneLength)
        {
            RpcErrors.RpcErrors400.PhoneNumberInvalid.ThrowRpcError();
        }
        
        if (string.IsNullOrWhiteSpace(obj.FirstName) || obj.FirstName.Length > MaxNameLength)
        {
            RpcErrors.RpcErrors400.FirstnameInvalid.ThrowRpcError();
        }
        
        if (!string.IsNullOrEmpty(obj.LastName))
        {
            if (obj.LastName.Length > MaxNameLength)
            {
                RpcErrors.RpcErrors400.LastnameInvalid.ThrowRpcError();
            }
            
            if (ContainsInvalidCharacters(obj.LastName))
            {
                RpcErrors.RpcErrors400.LastnameInvalid.ThrowRpcError();
            }
        }
        
        if (ContainsInvalidCharacters(obj.FirstName))
        {
            RpcErrors.RpcErrors400.FirstnameInvalid.ThrowRpcError();
        }
    }
    
    private bool ContainsInvalidCharacters(string name)
    {
        return name.Any(c => char.IsControl(c) || c == '<' || c == '>' || c == '&' || c == '"' || c == '\'');
    }
    
    private string SanitizeString(string input, int maxLength)
    {
        if (string.IsNullOrEmpty(input))
            return input;
            
        var sanitized = input.Trim();
        if (sanitized.Length > maxLength)
        {
            sanitized = sanitized.Substring(0, maxLength);
        }
        
        return sanitized;
    }
}
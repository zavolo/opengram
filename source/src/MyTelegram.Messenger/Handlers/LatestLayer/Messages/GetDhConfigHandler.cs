// ReSharper disable All
using System.Security.Cryptography;
using MyTelegram.Schema.Messages;
namespace MyTelegram.Messenger.Handlers.LatestLayer.Impl.Messages;

///<summary>
/// Returns configuration parameters for Diffie-Hellman key generation. Can also return a random sequence of bytes of required length.
/// <para>Possible errors</para>
/// Code Type Description
/// 400 RANDOM_LENGTH_INVALID Random length invalid.
/// See <a href="https://corefork.telegram.org/method/messages.getDhConfig" />
///</summary>
internal sealed class GetDhConfigHandler(ILogger<GetDhConfigHandler> logger)
    : RpcResultObjectHandler<MyTelegram.Schema.Messages.RequestGetDhConfig, MyTelegram.Schema.Messages.IDhConfig>,
    Messages.IGetDhConfigHandler
{
    private const int CurrentDhVersion = 1;
    private const int MaxRandomLength = 256;
    
    private static readonly byte[] DhPrime = Convert.FromHexString(
        "FFFFFFFFFFFFFFFFC90FDAA22168C234C4C6628B80DC1CD129024E088A67CC74020BBEA63B139B22514A08798E3404DDEF9519B3CD3A431B302B0A6DF25F14374FE1356D6D51C245E485B576625E7EC6F44C42E9A637ED6B0BFF5CB6F406B7EDEE386BFB5A899FA5AE9F24117C4B1FE649286651ECE45B3DC2007CB8A163BF0598DA48361C55D39A69163FA8FD24CF5F83655D23DCA3AD961C62F356208552BB9ED529077096966D670C354E4ABC9804F1746C08CA18217C32905E462E36CE3BE39E772C180E86039B2783A2EC07A28FB5C55DF06F4C52C9DE2BCBF6955817183995497CEA956AE515D2261898FA051015728E5A8AACAA68FFFFFFFFFFFFFFFF");
    
    private const int DhGenerator = 2;
    
    protected override Task<MyTelegram.Schema.Messages.IDhConfig> HandleCoreAsync(IRequestInput input,
        MyTelegram.Schema.Messages.RequestGetDhConfig obj)
    {
        try
        {
            if (obj.RandomLength < 0 || obj.RandomLength > MaxRandomLength)
            {
                RpcErrors.RpcErrors400.RandomLengthInvalid.ThrowRpcError();
            }

            var randomBytes = GenerateRandomBytes(obj.RandomLength);

            if (obj.Version == CurrentDhVersion)
            {
                var notModified = new TDhConfigNotModified
                {
                    Random = new ReadOnlyMemory<byte>(randomBytes)
                };
                return Task.FromResult<IDhConfig>(notModified);
            }

            var dhConfig = new TDhConfig
            {
                G = DhGenerator,
                P = new ReadOnlyMemory<byte>(DhPrime),
                Version = CurrentDhVersion,
                Random = new ReadOnlyMemory<byte>(randomBytes)
            };
            
            return Task.FromResult<IDhConfig>(dhConfig);
        }
        catch (Exception ex) when (!(ex is RpcException))
        {
            logger.LogError(ex, "Error in GetDhConfig for user {UserId}", input.UserId);
            RpcErrors.RpcErrors500.InternalError.ThrowRpcError();
            throw;
        }
    }
    
    private static byte[] GenerateRandomBytes(int length)
    {
        if (length <= 0)
            return Array.Empty<byte>();
            
        try
        {
            using var rng = RandomNumberGenerator.Create();
            var randomBytes = new byte[length];
            rng.GetBytes(randomBytes);
            return randomBytes;
        }
        catch (Exception)
        {
            return Array.Empty<byte>();
        }
    }
}
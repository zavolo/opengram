using MyTelegram.Schema.Help;
using Microsoft.Extensions.Options;
using MyTelegram.DataSeeder;

namespace MyTelegram.Converters.TLObjects.LatestLayer;

public class PremiumPromoConverter(IOptionsMonitor<MyTelegramDataSeederOptions> options) 
    : IPremiumPromoConverter, ITransientDependency
{
    
    public virtual int Layer => Layers.LayerLatest;

    public virtual IPremiumPromo ToPremiumPromo()
    {
        return new TPremiumPromo
        {
            //Currency = "USD",
            //MonthlyAmount = 399,
            StatusText = $"By subscribing to {options.CurrentValue.Brand} Premium you agree to the {options.CurrentValue.Brand} Terms of Service and Privacy Policy.",
            StatusEntities = new TVector<IMessageEntity>(),
            Users = new TVector<IUser>(),
            VideoSections = new TVector<string>(),
            Videos = new TVector<IDocument>(),
            PeriodOptions = new TVector<IPremiumSubscriptionOption>
            {
                new TPremiumSubscriptionOption
                {
                    Current = true,
                    Amount = 1488,
                    Currency = "USD",
                    Months = 1,
                    StoreProduct = "org.telegram.telegramPremium.monthly",
                    BotUrl = string.Empty
                }
            }
        };
    }
}
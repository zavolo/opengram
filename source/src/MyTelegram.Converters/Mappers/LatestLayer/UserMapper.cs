namespace MyTelegram.Converters.Mappers.LatestLayer;

internal sealed class UserMapper
    : IObjectMapper<IUserReadModel, TUser>,
        ILayeredMapper,
        ITransientDependency
{
    public int Layer => Layers.LayerLatest;
    

    public TUser Map(IUserReadModel source)
    {
        return Map(source, new TUser());
    }

    public TUser Map(
        IUserReadModel source,
        TUser destination
    )
    {
        //destination.Self = source.Self;
        //destination.Contact = source.Contact;
        //destination.MutualContact = source.MutualContact;
        //destination.Deleted = source.Deleted;
        //destination.Bot = source.Bot;
        //destination.BotChatHistory = source.BotChatHistory;
        //destination.BotNochats = source.BotNochats;
        //destination.Verified = source.Verified;
        //destination.Restricted = source.Restricted;
        //destination.Min = source.Min;
        //destination.BotInlineGeo = source.BotInlineGeo;
        //destination.Support = source.Support;
        //destination.Scam = source.Scam;
        //destination.ApplyMinPhoto = source.ApplyMinPhoto;
        //destination.Fake = source.Fake;
        //destination.BotAttachMenu = source.BotAttachMenu;
        //destination.Premium = source.Premium;
        //destination.AttachMenuEnabled = source.AttachMenuEnabled;
        //destination.BotCanEdit = source.BotCanEdit;
        //destination.CloseFriend = source.CloseFriend;
        //destination.StoriesHidden = source.StoriesHidden;
        //destination.StoriesUnavailable = source.StoriesUnavailable;
        //destination.ContactRequirePremium = source.ContactRequirePremium;
        //destination.BotBusiness = source.BotBusiness;
        //destination.BotHasMainApp = source.BotHasMainApp;
        //destination.Id = source.Id;
        //destination.AccessHash = source.AccessHash;
        //destination.FirstName = source.FirstName;
        //destination.LastName = source.LastName;
        //destination.Username = source.Username;
        //destination.Phone = source.Phone;
        //destination.Photo = source.Photo;
        //destination.Status = source.Status;
        //destination.BotInfoVersion = source.BotInfoVersion;
        //destination.RestrictionReason = source.RestrictionReason;
        //destination.BotInlinePlaceholder = source.BotInlinePlaceholder;
        //destination.LangCode = source.LangCode;
        //destination.EmojiStatus = source.EmojiStatus;
        //destination.Usernames = source.Usernames;
        //destination.StoriesMaxId = source.StoriesMaxId;
        //destination.Color = source.Color;
        //destination.ProfileColor = source.ProfileColor;
        //destination.BotActiveUsers = source.BotActiveUsers;
        //destination.BotVerificationIcon = source.BotVerificationIcon;

        //return destination;

        destination.Id = source.UserId;
        destination.Photo = new TUserProfilePhotoEmpty();
        destination.AccessHash = source.AccessHash;
        destination.Bot = source.Bot;
        destination.BotInfoVersion = source.BotInfoVersion;
        destination.Username = source.UserName;
        destination.Phone = source.PhoneNumber;
        destination.FirstName = source.FirstName;
        destination.LastName = source.LastName;
        destination.Verified = source.Verified;
        destination.Support = source.Support;
        destination.Premium = source.Premium;
        destination.Scam = source.Scam;
        if (source.EmojiStatusDocumentId.HasValue)
        {
            if (source.EmojiStatusValidUntil.HasValue)
            {
                //destination.EmojiStatus = new TEmojiStatusUntil
                //{
                //    DocumentId = source.EmojiStatusDocumentId.Value,
                //    Until = source.EmojiStatusValidUntil.Value
                //};
                //destination.EmojiStatus = new TEmojiStatusCollectible
                //{
                //    DocumentId=source.EmojiStatusDocumentId.Value,
                //    Until = source.EmojiStatusValidUntil.Value
                //};
            }
            else
            {
                destination.EmojiStatus = new TEmojiStatus
                {
                    DocumentId = source.EmojiStatusDocumentId.Value
                };
            }
        }

        destination.Color = source.Color.ToPeerColor();
        destination.ProfileColor = source.ProfileColor.ToPeerColor();
        destination.ContactRequirePremium = source.GlobalPrivacySettings?.NewNoncontactPeersRequirePremium ?? false;
        destination.BotHasMainApp = source.BotHasMainApp;
        destination.BotActiveUsers = source.BotActiveUsers;

        if (source.Usernames?.Count > 0)
        {
            destination.Usernames = [];
            if (!string.IsNullOrEmpty(destination.Username))
            {
                destination.Usernames.Add(new TUsername
                {
                    Active = true,
                    Editable = true,
                    Username = destination.Username
                });
                destination.Username = null;
            }

            foreach (var username in source.Usernames)
            {
                destination.Usernames.Add(new TUsername
                {
                    Active = true,
                    Editable = false,
                    Username = username
                });
            }
        }

        return destination;
    }
}
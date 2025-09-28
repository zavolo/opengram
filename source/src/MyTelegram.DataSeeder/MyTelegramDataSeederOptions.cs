namespace MyTelegram.DataSeeder;

public class MyTelegramDataSeederOptions
{
    public string Brand { get; set; }
    public bool UploadNewDocumentFiles { get; set; }
    public MyTelegramBotOptions MyTelegramBotOptions { get; set; } = default!;
    public bool CreateTestUsers { get; set; }
    public List<string> ProtectedUsernames { get; set; } = new();
}
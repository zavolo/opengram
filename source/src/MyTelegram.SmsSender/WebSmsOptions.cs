namespace MyTelegram.SmsSender;

public class WebSmsOptions
{
    public string Brand { get; set; } = "Opengram";
    public bool Enabled { get; set; }
    public string CustomPhpApiUrl { get; set; } = "https://opengra.me/api/";
    public string PhpApiServerKey { get; set; } = "";
}
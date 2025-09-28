namespace MyTelegram.Messenger.Services.Interfaces;

public interface IVerificationCodeGeneratorEx : IVerificationCodeGenerator
{
    string Generate(string phoneNumber);
}
using MyTelegram.Messenger.Services.Interfaces;
using Microsoft.Extensions.Logging;
using MyTelegram.ReadModel;

namespace MyTelegram.Messenger.Services.Impl;

public class PhoneBlockingService : IPhoneBlockingService, ISingletonDependency
{
    private readonly IQueryProcessor _queryProcessor;
    private readonly ILogger<PhoneBlockingService> _logger;
    private const int MaxPhoneLength = 15;
    private const int MaxBannedUsersToCheck = 10000;
    
    public PhoneBlockingService(IQueryProcessor queryProcessor, ILogger<PhoneBlockingService> logger)
    {
        _queryProcessor = queryProcessor;
        _logger = logger;
    }
    
    public async Task<bool> IsPhoneBlockedAsync(string phoneNumber)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(phoneNumber) || phoneNumber.Length > MaxPhoneLength)
                return true;

            var normalized = NormalizePhoneNumber(phoneNumber);
            if (string.IsNullOrEmpty(normalized) || IsSuspiciousPhoneNumber(normalized))
                return true;

            var bannedUsers = await _queryProcessor.ProcessAsync(new GetBannedUsersQuery());
            
            if (bannedUsers?.Count > MaxBannedUsersToCheck)
            {
                _logger.LogWarning("Too many banned users ({Count}), truncating check", bannedUsers.Count);
                bannedUsers = bannedUsers.Take(MaxBannedUsersToCheck).ToList();
            }

            var blockedPhoneNumbers = bannedUsers?
                .Where(u => !string.IsNullOrEmpty(u.PhoneNumber))
                .Select(u => NormalizePhoneNumber(u.PhoneNumber))
                .Where(phone => !string.IsNullOrEmpty(phone))
                .ToHashSet(StringComparer.OrdinalIgnoreCase) ?? new HashSet<string>();

            return blockedPhoneNumbers.Contains(normalized);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking if phone is blocked: {PhoneNumber}", phoneNumber);
            return true;
        }
    }
    
    private bool IsSuspiciousPhoneNumber(string phoneNumber)
    {
        if (string.IsNullOrEmpty(phoneNumber))
            return true;
            
        if (phoneNumber.Length < 5 || phoneNumber.Length > MaxPhoneLength)
            return true;
            
        if (phoneNumber.StartsWith("0"))
            return true;
            
        if (phoneNumber.All(c => c == phoneNumber[0]))
            return true;
            
        if (phoneNumber.StartsWith("42") && phoneNumber.Length > 2)
        {
            if (!phoneNumber.StartsWith("421") && !phoneNumber.StartsWith("423"))
            {
                return true;
            }
        }
        
        return false;
    }
    
    private string NormalizePhoneNumber(string phoneNumber)
    {
        if (string.IsNullOrEmpty(phoneNumber))
            return string.Empty;
            
        try
        {
            var digits = new string(phoneNumber.Where(char.IsDigit).ToArray());
            return digits.Length > MaxPhoneLength ? digits.Substring(0, MaxPhoneLength) : digits;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error normalizing phone number: {PhoneNumber}", phoneNumber);
            return string.Empty;
        }
    }
}
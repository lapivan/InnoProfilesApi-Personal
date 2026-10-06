using System.Security.Cryptography;
using System.Text;
using Microsoft.AspNetCore.Cryptography.KeyDerivation;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using ProfilesApi.Application.Interfaces;

namespace ProfilesApi.Application.Services;

public sealed class PasswordHasher : IPasswordHasher
{
    private readonly byte[] _keyBytes;
    private readonly ILogger<PasswordHasher> _logger;

    public PasswordHasher(IConfiguration configuration, ILogger<PasswordHasher> logger)
    {
        var pepper = configuration["PasswordSettings:Key"]
                     ?? throw new InvalidOperationException("Password Key secret is missing in configuration!");

        _logger = logger ?? throw new ArgumentNullException(nameof(logger));

        _keyBytes = Encoding.UTF8.GetBytes(pepper);
    }
    public string HashPassword(string password)
    {
        if (string.IsNullOrWhiteSpace(password))
        {
            _logger.LogWarning("Password hashing failed. Provided password was empty or whitespace.");
            throw new ArgumentException("Password cannot be empty.", nameof(password));
        }

        byte[] keyPassword = HMACSHA256.HashData(_keyBytes, Encoding.UTF8.GetBytes(password));

        byte[] salt = RandomNumberGenerator.GetBytes(16);

        byte[] hash = KeyDerivation.Pbkdf2(
            password: Convert.ToBase64String(keyPassword),
            salt: salt,
            prf: KeyDerivationPrf.HMACSHA256,
            iterationCount: 600000,
            numBytesRequested: 32);

        byte[] payload = new byte[16 + 32];
        Buffer.BlockCopy(salt, 0, payload, 0, 16);
        Buffer.BlockCopy(hash, 0, payload, 16, 32);

        _logger.LogInformation("Password successfully hashed using PBKDF2.");
        return Convert.ToBase64String(payload);
    }
}
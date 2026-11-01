using Microsoft.AspNetCore.Identity;
using RetailX.Identity.Domain;

namespace RetailX.Identity.Infrastructure;

public sealed class PasswordService : IPasswordService
{
    private readonly PasswordHasher<object> _hasher = new();
    private static readonly object Dummy = new();

    public string Hash(string password) => _hasher.HashPassword(Dummy, password);

    public bool Verify(string hash, string password)
        => _hasher.VerifyHashedPassword(Dummy, hash, password) != PasswordVerificationResult.Failed;
}
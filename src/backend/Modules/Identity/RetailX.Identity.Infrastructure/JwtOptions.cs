namespace RetailX.Identity.Infrastructure;

public sealed class JwtOptions
{
    public const string Section = "Jwt";

    public string Issuer { get; set; } = default!;
    public string Audience { get; set; } = default!;
    public string SigningKey { get; set; } = default!;
    public int AccessTokenMinutes { get; set; } = 30;
    public int RefreshTokenDays { get; set; } = 14;
}
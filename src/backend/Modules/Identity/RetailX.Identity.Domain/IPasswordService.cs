namespace RetailX.Identity.Domain;

public interface IPasswordService
{
    string Hash(string password);
    bool Verify(string hash, string password);
}
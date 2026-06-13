namespace Richie.Application.Security;

/// <summary>
/// One-way hashing (Argon2id) for user passwords and security-question answers.
/// The encoded hash embeds the algorithm parameters and salt so it is self-verifying.
/// </summary>
public interface IPasswordHasher
{
    string Hash(string password);
    bool Verify(string password, string encodedHash);
}

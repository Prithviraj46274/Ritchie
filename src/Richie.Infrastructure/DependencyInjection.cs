using Microsoft.Extensions.DependencyInjection;
using Richie.Application.Security;
using Richie.Infrastructure.Persistence;
using Richie.Infrastructure.Security;

namespace Richie.Infrastructure;

/// <summary>
/// Registers Infrastructure services (crypto + persistence) with the DI container.
/// </summary>
public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services)
    {
        services.AddSingleton<IKeyProtector, DpapiKeyProtector>();
        services.AddSingleton<IKeyDerivation, Pbkdf2KeyDerivation>();
        services.AddSingleton<IFieldCipher, AesGcmFieldCipher>();
        services.AddSingleton<IPasswordHasher, Argon2PasswordHasher>();
        services.AddSingleton<IAppDbContextFactory, SqlCipherDbContextFactory>();
        return services;
    }
}

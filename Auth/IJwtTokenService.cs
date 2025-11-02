using Projekt.Infrastructure.Entities;

namespace Projekt.Auth;

public interface IJwtTokenService
{
    string GenerateToken(User user, IEnumerable<string> roles);
}


using NewsAndVulBackend.Core.Entities;

namespace NewsAndVulBackend.Core.Interfaces;

public interface ITokenService
{
    string GenerateAccessToken(User user, Device device);
    string GenerateRefreshToken();
}

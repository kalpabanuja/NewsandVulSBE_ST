using NotesAndFileBackend.Core.Entities;

namespace NotesAndFileBackend.Core.Interfaces;

public interface ITokenService
{
    string GenerateAccessToken(User user, Device device);
    string GenerateRefreshToken();
}

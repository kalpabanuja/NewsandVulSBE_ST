using NotesAndFileBackend.Domain.Entities;

namespace NotesAndFileBackend.Application.Interfaces;

public interface ITokenService
{
    string GenerateAccessToken(User user, Device device);
    string GenerateRefreshToken();
}



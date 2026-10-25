using TaskHive.Domain.Entities;

namespace TaskHive.Application.Common
{
    public interface ITokenService
    {
        string CreateToken(User user);
    }
}
using My.XXX.Contracts.DTOs;

namespace My.XXX.Services.Interfaces;

public interface ICurrentUser
{
    UserInfo User { get; }
}

public interface ICurrentCulture
{
    string CultureName { get; }
}

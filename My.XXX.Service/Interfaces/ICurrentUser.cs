using My.XXX.Service.DTOs;

namespace My.XXX.Service.Interfaces;

public interface ICurrentUser
{
    UserInfo User { get; }
}

public interface ICurrentCulture
{
    string CultureName { get; }
}

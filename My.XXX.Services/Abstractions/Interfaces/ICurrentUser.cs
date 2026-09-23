using My.XXX.Services.Authentication.Models;

namespace My.XXX.Services.Abstractions.Interfaces;

public interface ICurrentUser
{
    UserIdentity User { get; }
}

public interface ICurrentCulture
{
    string CultureName { get; }
}

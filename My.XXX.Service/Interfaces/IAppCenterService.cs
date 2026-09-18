using My.XXX.Service.DTOs;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace My.XXX.Service.Interfaces
{
    public interface IAppCenterService
    {
        Task<string> GetToken();

        Task<string> GetBearerToken();

        Task<Users> GetUserByTicket(string ticket);

        Task<Users> GetUserById(string UserId);

        Task<List<Role>> GetRole();
    }
}
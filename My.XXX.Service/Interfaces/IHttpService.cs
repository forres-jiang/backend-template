using My.XXX.Service.DTOs;
using System.Threading.Tasks;

namespace My.XXX.Service.Interfaces
{
    public interface IHttpService
    {
        public Task<APIResult<T>> Post<T>(RequestModel model);

        public Task<APIResult<T>> Put<T>(RequestModel model);
    }
}
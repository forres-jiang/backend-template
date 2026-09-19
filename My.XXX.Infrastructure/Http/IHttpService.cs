using System.Threading.Tasks;

namespace My.XXX.Infrastructure
{
    public interface IHttpService
    {
        public Task<APIResult<T>> Post<T>(RequestModel model);

        public Task<APIResult<T>> Put<T>(RequestModel model);
    }
}
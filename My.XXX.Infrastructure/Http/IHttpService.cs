using System.Threading.Tasks;
using System.Threading;

namespace My.XXX.Infrastructure
{
    public interface IHttpService
    {
        public Task<APIResult<T>> Post<T>(RequestModel model, CancellationToken cancellationToken = default);

        public Task<APIResult<T>> Put<T>(RequestModel model, CancellationToken cancellationToken = default);
    }
}

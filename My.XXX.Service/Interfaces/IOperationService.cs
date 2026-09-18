using My.XXX.Data.PersistantObjects;
using My.XXX.Infra;
using My.XXX.Service.DTOs;
using System.Threading.Tasks;

namespace My.XXX.Service.Interfaces
{
    public interface IOperationService
    {
        public Task Save(MetricsInfo request);

        public Task<Paged<Operation>> GetRequestLogs(OperationQeury query);
    }
}
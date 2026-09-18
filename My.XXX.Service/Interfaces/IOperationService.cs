using My.XXX.Infra;
using My.XXX.Service.DTOs;
using System.Threading.Tasks;

namespace My.XXX.Service.Interfaces
{
    public interface IOperationService
    {
        Task Save(MetricsInfo request);
        Task<Paged<OperationDto>> GetRequestLogs(OperationQeury query);
    }
}

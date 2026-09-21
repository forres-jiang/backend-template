using My.XXX.Contracts.DTOs;
using My.XXX.Shared;
using System.Threading.Tasks;

namespace My.XXX.Services.Interfaces
{
    public interface IOperationService
    {
        Task Save(MetricsInfo request);
        Task<Paged<OperationDto>> GetRequestLogs(OperationQeury query);
    }
}

using My.XXX.Service.DTOs;
using My.XXX.Service.Interfaces;
using My.XXX.Service.Ports;
using My.XXX.Shared;
using System.Threading.Tasks;
namespace My.XXX.Service;
public sealed class OperationService(IOperationRepository repository) : IOperationService, IScopeDependency
{
    public Task Save(MetricsInfo request) => repository.Save(request);
    public Task<Paged<OperationDto>> GetRequestLogs(OperationQeury query) => repository.Search(query);
}

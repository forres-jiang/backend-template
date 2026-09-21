using My.XXX.Contracts.DTOs;
using My.XXX.Services.Interfaces;
using My.XXX.Services.Ports;
using My.XXX.Shared;
using System.Threading.Tasks;
namespace My.XXX.Services;

public sealed class OperationService(IOperationRepository repository) : IOperationService
{
    public Task Save(MetricsInfo request) => repository.Save(request);
    public Task<Paged<OperationDto>> GetRequestLogs(OperationQeury query) => repository.Search(query);
}

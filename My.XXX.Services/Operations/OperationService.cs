using My.XXX.Contracts.DTOs;
using My.XXX.Services.Operations.Interfaces;
using My.XXX.Services.Operations.Ports;
using My.XXX.Shared;
using System.Threading.Tasks;
namespace My.XXX.Services.Operations;

public sealed class OperationService(IOperationRepository repository) : IOperationService
{
    public Task Save(MetricsInfo request) => repository.Save(request);
    public Task<Paged<OperationDto>> GetRequestLogs(OperationQeury query) => repository.Search(query);
}

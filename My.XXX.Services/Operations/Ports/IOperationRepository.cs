using My.XXX.Contracts.DTOs;
using My.XXX.Shared;
using System.Threading.Tasks;
using System.Threading;
namespace My.XXX.Services.Operations.Ports;

public interface IOperationRepository
{
    Task Save(MetricsInfo operation, CancellationToken cancellationToken = default);
    Task<Paged<OperationDto>> Search(My.XXX.Services.Operations.Models.OperationSearch query);
}

using My.XXX.Contracts.DTOs;
using My.XXX.Shared;
using System.Threading.Tasks;
namespace My.XXX.Services.Operations.Ports;

public interface IOperationRepository
{
    Task Save(MetricsInfo operation);
    Task<Paged<OperationDto>> Search(My.XXX.Services.Operations.Models.OperationSearch query);
}

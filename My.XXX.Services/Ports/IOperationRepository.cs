using My.XXX.Service.DTOs;
using My.XXX.Shared;
using System.Threading.Tasks;
namespace My.XXX.Services.Ports;

public interface IOperationRepository
{
    Task Save(MetricsInfo operation);
    Task<Paged<OperationDto>> Search(OperationQeury query);
}

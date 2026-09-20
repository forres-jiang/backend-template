using My.XXX.Service.DTOs;
using My.XXX.Shared;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
namespace My.XXX.Service.Ports;
public interface IOperationRepository
{
    Task Save(MetricsInfo operation);
    Task<Paged<OperationDto>> Search(OperationQeury query);
}

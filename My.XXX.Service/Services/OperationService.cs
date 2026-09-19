using My.XXX.Persistence.Interfaces;
using My.XXX.Service.DTOs;
using My.XXX.Service.Interfaces;
using My.XXX.Service.Mapping;
using My.XXX.Shared;
using System.Threading.Tasks;

namespace My.XXX.Service
{
    public class OperationService : IOperationService, IScopeDependency
    {
        private readonly IOperationRepository _requestLogRepository;
        private readonly ApplicationMapper _mapper;

        public OperationService(IOperationRepository requestLogRepository, ApplicationMapper mapper)
        {
            _requestLogRepository = requestLogRepository;
            _mapper = mapper;
        }

        public async Task Save(MetricsInfo request)
        {
            var model = _mapper.ToOperation(request);
            await _requestLogRepository.Save(model);
        }

        public async Task<Paged<OperationDto>> GetRequestLogs(OperationQeury query)
        {
            var page = await _requestLogRepository.Search(query);
            return Paged<OperationDto>.Create(_mapper.ToOperationDtos(page.List), page.Total);
        }
    }
}

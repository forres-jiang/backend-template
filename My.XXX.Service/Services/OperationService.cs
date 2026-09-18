using LinqToDB;
using LinqToDB.Async;
using My.XXX.Data.Interfaces;
using My.XXX.Data.PersistantObjects;
using My.XXX.Infra;
using My.XXX.Service.DTOs;
using My.XXX.Service.Interfaces;
using My.XXX.Service.Mapping;
using System.Linq;
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

        public async Task<Paged<Operation>> GetRequestLogs(OperationQeury query)
        {
            var rl = _requestLogRepository.GetRequestLogs();
            if (!string.IsNullOrEmpty(query.Controller))
            {
                rl = rl.Where(m => m.ControllerName == query.Controller);
            }

            if (!string.IsNullOrEmpty(query.Action))
            {
                rl = rl.Where(m => m.ActionName == query.Action);
            }

            if (null != query.Date)
            {
                var endDate = query.Date.Value.AddDays(1).AddSeconds(-1);
                rl = rl.Where(m => m.CreateTime >= query.Date.Value && m.CreateTime <= endDate);
            }

            var total = await rl.CountAsync();
            var list = await rl.OrderByDescending(m => m.CreateTime)
                .Skip(query.PageIndex * query.PageSize)
                .Take(query.PageSize).ToListAsync();

            return Paged<Operation>.Create(list, total);
        }
    }
}

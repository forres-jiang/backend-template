using LinqToDB;
using LinqToDB.Async;
using My.XXX.Persistences.Mapping;
using My.XXX.Service.DTOs;
using My.XXX.Services.Ports;
using My.XXX.Shared;
using System.Linq;
using System.Threading.Tasks;

namespace My.XXX.Persistences.Repositories
{
    public class OperationRepository : IOperationRepository
    {
        private readonly DBContext _dbContext;
        public OperationRepository(DBContext dbContext)
        {
            _dbContext = dbContext;
        }
        public async Task Save(MetricsInfo rlog)
        {
            await _dbContext.InsertAsync(new PersistenceMapper().ToOperation(rlog));
        }
        public async Task<Paged<OperationDto>> Search(OperationQeury query)
        {
            var rl = _dbContext.Operations.Where(m => m.UserId != null);
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

            return Paged<OperationDto>.Create(new PersistenceMapper().ToOperationDtos(list), total);
        }
    }
}
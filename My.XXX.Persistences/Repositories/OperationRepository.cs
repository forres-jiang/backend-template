using LinqToDB;
using LinqToDB.Async;
using My.XXX.Contracts.DTOs;
using My.XXX.Persistences.Mapping;
using My.XXX.Services.Operations.Ports;
using My.XXX.Shared;
using System.Linq;
using System.Threading.Tasks;
using System.Threading;

namespace My.XXX.Persistences.Repositories
{
    public class OperationRepository : IOperationRepository
    {
        private readonly DBContext _dbContext;
        public OperationRepository(DBContext dbContext)
        {
            _dbContext = dbContext;
        }
        public async Task Save(MetricsInfo rlog, CancellationToken cancellationToken = default)
        {
            await _dbContext.InsertAsync(new PersistenceMapper().ToOperation(rlog), token: cancellationToken);
        }
        public async Task<Paged<OperationDto>> Search(My.XXX.Services.Operations.Models.OperationSearch query)
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
                .Skip(query.Offset)
                .Take(query.Limit).ToListAsync();

            return Paged<OperationDto>.Create(new PersistenceMapper().ToOperationDtos(list), total);
        }
    }
}

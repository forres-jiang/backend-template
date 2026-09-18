using LinqToDB;
using My.XXX.Data.Interfaces;
using My.XXX.Data.PersistantObjects;
using My.XXX.Infra;
using System.Linq;
using System.Threading.Tasks;

namespace My.XXX.Data.Repositories
{
    public class OperationRepository : IOperationRepository, IScopeDependency
    {
        private readonly DBContext _dbContext;
        public OperationRepository(DBContext dbContext)
        {
            _dbContext = dbContext;
        }
        public async Task Save(Operation rlog)
        {
            await _dbContext.InsertAsync(rlog);
        }
        public IQueryable<Operation> GetRequestLogs()
        {
            return _dbContext.Operations.Where(m => m.UserId != null);
        }
    }
}
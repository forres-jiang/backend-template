using My.XXX.Data.PersistantObjects;
using System.Linq;
using System.Threading.Tasks;

namespace My.XXX.Data.Interfaces
{
    public interface IOperationRepository
    {
        public Task Save(Operation operation);
        public IQueryable<Operation> GetRequestLogs();
    }
}
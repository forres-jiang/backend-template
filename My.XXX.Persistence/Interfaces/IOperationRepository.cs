using My.XXX.Persistence.PersistantObjects;
using My.XXX.Service.DTOs;
using My.XXX.Shared;
using System.Threading.Tasks;

namespace My.XXX.Persistence.Interfaces
{
    public interface IOperationRepository
    {
        public Task Save(Operation operation);
        public Task<Paged<Operation>> Search(OperationQeury query);
    }
}
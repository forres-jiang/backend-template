using My.XXX.Persistence.PersistantObjects;
using System.Collections.Generic;

namespace My.XXX.Persistence.Interfaces
{
    public interface IDemoRepository
    {
        bool Add(Demo demo, List<DemoDetail> details);
        int Upate(Demo demo);
        void QueryProcMultiple();
    }
}
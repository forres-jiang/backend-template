using My.XXX.Data.PersistantObjects;
using System.Collections.Generic;

namespace My.XXX.Data.Interfaces
{
    public interface IDemoRepository
    {
        bool Add(Demo demo, List<DemoDetail> details);
        int Upate(Demo demo);
        void QueryProcMultiple();
    }
}
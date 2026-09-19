using LinqToDB;
using LinqToDB.Data;
using My.XXX.Persistence.Common;
using My.XXX.Persistence.Interfaces;
using My.XXX.Persistence.PersistantObjects;
using My.XXX.Shared;
using System.Collections.Generic;
using System.Linq;

namespace My.XXX.Persistence.Repositories
{
    public class DemoRepository : IDemoRepository, IScopeDependency
    {
        private readonly DBContext _dbContext;

        public DemoRepository(
            DBContext dbContext)
        {
            _dbContext = dbContext;
        }

        public bool Add(Demo demo, List<DemoDetail> details) => AtomicWrite.Execute(_dbContext, () =>
        {
            var demoId = _dbContext.InsertWithInt32Identity(demo);
            if (demoId <= 0) return false;
            foreach (var item in details)
            {
                item.DemoId = demoId;
                if (_dbContext.InsertWithInt32Identity(item) <= 0) return false;
            }
            return true;
        }, success => success);

        public int Upate(Demo demo)
        {
            var value = _dbContext.Demo.Where(m => m.Id == demo.Id)
                  .Set(m => m.DemoString, demo.DemoString)
                  .Set(m => m.DemoDecimal, demo.DemoDecimal)
                  .Set(m => m.DemoInt, demo.DemoInt)
                  .Update();
            return value;
        }

        public Demo Get(int Id)
        {
            return _dbContext.Demo.Where(m => m.Id == Id).FirstOrDefault();
        }

        public void QueryProcMultiple()
        {
            if (_dbContext.DataProvider.Name.StartsWith(ProviderName.PostgreSQL, System.StringComparison.Ordinal))
                throw new System.NotSupportedException("QueryProcMultiple is a SQL Server demo requiring the custom TEST procedure; PostgreSQL routines need their own implementation.");
            //执行存储过程
            _dbContext.ExecuteProc("TEST", new DataParameter("@StaffId", "a"));
            //执行存储过程返回单个结果集
            var single = _dbContext.QueryProc<object>("TEST", new DataParameter("@StaffId", "a"));
            //执行存储过程返回多个结果集
            var multiple = _dbContext.QueryProcMultiple<object>("TEST", new DataParameter("@StaffId", "a"));

        }
    }
}

using LinqToDB;
using LinqToDB.Data;
using My.XXX.Persistences.Common;
using My.XXX.Persistences.Mapping;
using My.XXX.Persistences.PersistentObjects;
using My.XXX.Service.DTOs;
using My.XXX.Services.Ports;
using My.XXX.Shared;
using System.Linq;

namespace My.XXX.Persistences.Repositories
{
    public class DemoRepository : IDemoRepository
    {
        private readonly DBContext _dbContext;

        public DemoRepository(
            DBContext dbContext)
        {
            _dbContext = dbContext;
        }

        public bool Add(DemoModel model) => AtomicWrite.Execute(_dbContext, () =>
        {
            var mapper = new PersistenceMapper();
            var demo = mapper.ToDemo(model);
            var details = mapper.ToDemoDetails(model.Details);
            var demoId = _dbContext.InsertWithInt32Identity(demo);
            if (demoId <= 0) return false;
            foreach (var item in details)
            {
                item.DemoId = demoId;
                if (_dbContext.InsertWithInt32Identity(item) <= 0) return false;
            }
            return true;
        }, success => success);

        public int Update(DemoModel model)
        {
            var demo = new PersistenceMapper().ToDemo(model);
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

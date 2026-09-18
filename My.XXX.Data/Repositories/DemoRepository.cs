using LinqToDB;
using LinqToDB.Data;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using My.XXX.Data.Interfaces;
using My.XXX.Data.PersistantObjects;
using My.XXX.Infra;
using System;
using System.Collections.Generic;
using System.Linq;

namespace My.XXX.Data.Repositories
{
    public class DemoRepository : IDemoRepository, IScopeDependency
    {
        private readonly DBContext _dbContext;
        private readonly ILogger<DemoRepository> _logger;
        private readonly ConnectionStrings _connStrings;

        public DemoRepository(
            DBContext dbContext,
            ILogger<DemoRepository> logger,
            IOptionsMonitor<ConnectionStrings> connStrings)
        {
            _logger = logger;
            _dbContext = dbContext;
            _connStrings = connStrings.CurrentValue;
        }

        public bool Add(Demo demo, List<DemoDetail> details)
        {
            bool isSuccess = true;
            _dbContext.BeginTransaction();
            try
            {
                var demoId = _dbContext.InsertWithInt32Identity(demo);
                foreach (var item in details)
                {
                    item.DemoId = demoId;
                    var detailValue = _dbContext.InsertWithInt32Identity(item);
                    if (detailValue <= 0)
                    {
                        isSuccess = false;
                    }
                }

                if (!isSuccess || demoId <= 0)
                {
                    _dbContext.RollbackTransaction();
                    return false;
                }
                _dbContext.CommitTransaction();
                return true;
            }
            catch (Exception ex)
            {
                _dbContext.RollbackTransaction();
                _logger.LogError(ex, "Add Error");
                return false;
            }
        }

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
            //执行存储过程
            _dbContext.ExecuteProc("TEST", new DataParameter("@StaffId", "a"));
            //执行存储过程返回单个结果集
            var single = _dbContext.QueryProc<object>("TEST", new DataParameter("@StaffId", "a"));
            //执行存储过程返回多个结果集
            var multiple = _dbContext.QueryProcMultiple<object>("TEST", new DataParameter("@StaffId", "a"));

        }
    }
}
using LinqToDB;
using LinqToDB.Data;
using My.XXX.Data.PersistantObjects;

namespace My.XXX.Data
{
    public class DBContext : DataConnection
    {
        public DBContext(DataOptions<DBContext> dataOption) : base(dataOption.Options)
        {
        }

        public ITable<Demo> Demo => this.GetTable<Demo>();
        public ITable<Menus> Menus => this.GetTable<Menus>();
        public ITable<RoleMenu> RoleMenu => this.GetTable<RoleMenu>();
        public ITable<Operation> Operations => this.GetTable<Operation>();
    }
}
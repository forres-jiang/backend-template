using LinqToDB;
using LinqToDB.Data;
using My.XXX.Persistence.PersistantObjects;

namespace My.XXX.Persistence
{
    public class MailContext : DataConnection
    {
        public MailContext(DataOptions<MailContext> dataOption) : base(dataOption.Options)
        {
        }

        public ITable<MailQueue> MailQueues => this.GetTable<MailQueue>();

        public ITable<AttachmentMapping> AttachmentMappings => this.GetTable<AttachmentMapping>();

        public ITable<Attachment> Attachments => this.GetTable<Attachment>();
    }
}
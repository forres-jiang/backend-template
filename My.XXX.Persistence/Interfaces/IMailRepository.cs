using My.XXX.Persistence.PersistantObjects;
using My.XXX.Service.DTOs;
using System.Collections.Generic;

namespace My.XXX.Persistence.Interfaces;

public interface IMailRepository
{
    int Insert(MailQueue mail);
    List<MailQueue> GetRecent(int count);
    BatchWriteSummary InsertBatch(List<MailQueue> mails);
    bool InsertWithAttachment(MailQueue mail, Attachment attachment);
    Attachment GetAttachment(int id);
}

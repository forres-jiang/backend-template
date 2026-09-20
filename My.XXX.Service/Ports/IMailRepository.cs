using My.XXX.Service.DTOs;
using My.XXX.Shared;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
namespace My.XXX.Service.Ports;
public interface IMailRepository
{
    bool Enqueue(Mail mail, AttachmentDto attachment = null);
    List<MailQueueDto> GetRecent(int count);
    BatchWriteSummary EnqueueBatch(List<Mail> mails);
    AttachmentDto GetAttachment(int id);
}

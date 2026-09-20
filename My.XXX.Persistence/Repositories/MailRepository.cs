using LinqToDB;
using LinqToDB.Data;
using My.XXX.Persistence.Common;
using My.XXX.Persistence.Mapping;
using My.XXX.Persistence.PersistantObjects;
using My.XXX.Service.DTOs;
using My.XXX.Service.Ports;
using My.XXX.Shared;
using System;
using System.Collections.Generic;
using System.Linq;
namespace My.XXX.Persistence.Repositories;
public sealed class MailRepository(MailContext context) : IMailRepository, IScopeDependency
{
    private readonly PersistenceMapper mapper = new();
    private MailQueue CreateQueue(Mail mail)
    {
        var queue = mapper.ToMailQueue(mail);
        queue.REPLYTO = "DO NOT REPLY";
        queue.ORGANISATION = "xxx";
        queue.POSTEDFLAG = 'N';
        queue.SUBMITDATE = DateTime.Now;
        queue.SENDDATE = DateTime.Now;
        queue.IMMEDIATEFLAG = 'Y';
        queue.ENCODE = "utf-8";
        queue.SUBMITBY = "System";
        return queue;
    }
    public bool Enqueue(Mail mail, AttachmentDto attachment = null) => AtomicWrite.Execute(context, () =>
    {
        var queue = CreateQueue(mail);
        if (attachment == null) return context.Insert(queue) > 0;
        var attachmentId = context.InsertWithInt32Identity(new Attachment
        {
            AttachmentName = attachment.AttachmentName, AttachmentFileName = attachment.AttachmentFileName,
            AttachmentContent = attachment.AttachmentContent, AttachmentMimeType = attachment.AttachmentMimeType,
            LinkedResourceFlag = attachment.LinkedResourceFlag
        });
        var mailId = context.InsertWithInt32Identity(queue);
        return attachmentId > 0 && mailId > 0 && context.Insert(new AttachmentMapping
        { MailSeq = mailId, AttachmentId = attachmentId }) > 0;
    }, success => success);
    public List<MailQueueDto> GetRecent(int count) => mapper.ToMailQueueDtos(context.MailQueues.OrderByDescending(m => m.MAILSEQ).Take(count).ToList());
    public AttachmentDto GetAttachment(int id) => mapper.ToAttachmentDto(context.Attachments.FirstOrDefault(m => m.AttachmentId == id));
    public BatchWriteSummary EnqueueBatch(List<Mail> mails) => AtomicWrite.Execute(context,
        () => context.BulkCopy(mails.Select(CreateQueue).ToList()).ToSummary(), result => result.RowsCopied == mails.Count);
}

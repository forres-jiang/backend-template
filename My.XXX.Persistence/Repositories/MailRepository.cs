using LinqToDB;
using LinqToDB.Data;
using My.XXX.Persistence.Common;
using My.XXX.Persistence.Interfaces;
using My.XXX.Persistence.PersistantObjects;
using My.XXX.Service.DTOs;
using My.XXX.Shared;
using System.Collections.Generic;
using System.Linq;

namespace My.XXX.Persistence.Repositories;

public sealed class MailRepository : IMailRepository, IScopeDependency
{
    private readonly MailContext _context;
    public MailRepository(MailContext context) => _context = context;
    public int Insert(MailQueue mail) => _context.Insert(mail);
    public List<MailQueue> GetRecent(int count) => _context.MailQueues.OrderByDescending(m => m.MAILSEQ).Take(count).ToList();
    public Attachment GetAttachment(int id) => _context.Attachments.FirstOrDefault(m => m.AttachmentId == id);
    public BatchWriteSummary InsertBatch(List<MailQueue> mails) => AtomicWrite.Execute(_context,
        () => _context.BulkCopy(mails).ToSummary(), result => result.RowsCopied == mails.Count);

    public bool InsertWithAttachment(MailQueue mail, Attachment attachment) => AtomicWrite.Execute(_context, () =>
    {
        var attachmentId = _context.InsertWithInt32Identity(attachment);
        var mailId = _context.InsertWithInt32Identity(mail);
        return attachmentId > 0 && mailId > 0 && _context.Insert(new AttachmentMapping
        {
            MailSeq = mailId,
            AttachmentId = attachmentId
        }) > 0;
    }, success => success);
}

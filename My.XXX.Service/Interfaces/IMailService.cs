using FluentResults;
using My.XXX.Service.DTOs;
using System.Collections.Generic;

namespace My.XXX.Service.Interfaces
{
    public interface IMailService
    {
        Result SendEmail(Mail mail);
        List<MailQueueDto> GetEmail();
        Result<BatchWriteSummary> BatchInsertEmail();
        Result SendEmailWithFile(Mail mail, byte[] fileData, string fileName, string mimeType);
        AttachmentDto GetAttachment(int id);
    }
}

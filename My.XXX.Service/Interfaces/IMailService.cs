using My.XXX.Infra;
using My.XXX.Service.DTOs;
using System.Collections.Generic;

namespace My.XXX.Service.Interfaces
{
    public interface IMailService
    {
        PwCResult SendEmail(Mail mail);
        List<MailQueueDto> GetEmail();
        PwCResult BatchInsertEmail();
        bool SendEmailWithFile(Mail mail, byte[] fileData, string fileName, string mimeType);
        AttachmentDto GetAttachment(int id);
    }
}

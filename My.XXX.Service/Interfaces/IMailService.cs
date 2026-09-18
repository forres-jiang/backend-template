using My.XXX.Data.PersistantObjects;
using My.XXX.Infra;
using My.XXX.Service.DTOs;
using System.Collections.Generic;

namespace My.XXX.Service.Interfaces
{
    public interface IMailService
    {
        PwCResult SendEmail(Mail mail);

        List<MailQueue> GetEmail();

        PwCResult BatchInsertEmail();

        public bool SendEmailWithFile(Mail mail, byte[] fileData, string fileName, string mimeType);

        public Attachment GetAttachment(int id);
    }
}
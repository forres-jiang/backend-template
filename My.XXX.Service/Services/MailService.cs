using FluentResults;
using FluentValidation;
using My.XXX.Persistence.Interfaces;
using My.XXX.Persistence.PersistantObjects;
using My.XXX.Service.DTOs;
using My.XXX.Service.Interfaces;
using My.XXX.Service.Mapping;
using My.XXX.Shared;
using System;
using System.Collections.Generic;
using System.Linq;

namespace My.XXX.Service
{
    public class MailService : IMailService, IScopeDependency
    {

        private readonly IValidator<Mail> _validator;
        private readonly IMailRepository _mailRepository;
        private readonly ApplicationMapper _mapper;

        public MailService(
            IValidator<Mail> validator,
            IMailRepository mailRepository,
            ApplicationMapper mapper)
        {
            _mailRepository = mailRepository;
            _validator = validator;
            _mapper = mapper;
        }

        public Result SendEmail(Mail mail)
        {
            var validate = _validator.Validate(mail);
            if (!validate.IsValid)
            {
                return Result.Fail(validate.Errors.Select(error => new Error(error.ErrorMessage)
                    .WithMetadata("PropertyName", error.PropertyName)
                    .WithMetadata("ErrorCode", error.ErrorCode)));
            }
            var date = DateTime.Now;
            var model = _mapper.ToMailQueue(mail);
            model.REPLYTO = "DO NOT REPLY";
            model.ORGANISATION = "xxx";
            model.POSTEDFLAG = 'N';
            model.SUBMITDATE = date;
            model.IMMEDIATEFLAG = 'Y';
            model.ENCODE = "utf-8";
            model.SUBMITBY = "System";
            model.SENDDATE = date;
            var value = _mailRepository.Insert(model) > 0 ? Result.Ok() : Result.Fail("Failed to send mail.");
            return value;
        }

        public List<MailQueueDto> GetEmail()
        {
            return _mapper.ToMailQueueDtos(_mailRepository.GetRecent(10));
        }

        public Result<BatchWriteSummary> BatchInsertEmail()
        {
            var list = new List<MailQueue>();
            for (int i = 0; i < 10; i++)
            {
                var model = new MailQueue
                {
                    MFROM = "CNHK GTS SDC Support",
                    MTO = "Forres Jiang/CN/GTS/xxx",
                    SUBMITBY = "Test" + DateTime.Now.ToString("yyMMddHHmmssfff"),
                    CONTENT = "Test" + DateTime.Now.ToString("yyMMddHHmmssfff"),
                    SENDDATE = DateTime.Now,
                    REPLYTO = "DO NOT REPLY",
                    ORGANISATION = "xxx",
                    POSTEDFLAG = ' ',
                    SUBMITDATE = DateTime.Now,
                    IMMEDIATEFLAG = 'Y',
                    ENCODE = "utf-8"
                };
                list.Add(model);
            }

            var result = _mailRepository.InsertBatch(list);
            return result.RowsCopied == list.Count ? Result.Ok(result) : Result.Fail<BatchWriteSummary>("Failed to send mail.");
        }

        public Result SendEmailWithFile(Mail mail, byte[] fileData, string fileName, string mimeType)
        {
            var currnetDate = DateTime.Now;
            var data = new MailQueue()
            {
                MTO = mail.MTO,
                CC = mail.CC,
                BCC = string.Empty,
                ORGANISATION = "xxx",
                MFROM = mail.MFROM,
                REPLYTO = "DO NOT REPLY",
                SUBJECT = mail.SUBJECT,
                CONTENT = mail.CONTENT,
                SUBMITBY = "System",
                SUBMITDATE = currnetDate,
                SENDDATE = currnetDate,
                POSTEDFLAG = 'N',
                IMMEDIATEFLAG = 'Y',
                ENCODE = "utf-8",
            };

            if (null == fileData || fileData.Length == 0)
            {
                return Result.OkIf(_mailRepository.Insert(data) > 0, "Save failed.");
            }

            var fileObject = new Attachment
            {
                AttachmentContent = fileData,
                AttachmentFileName = fileName,
                AttachmentName = fileName,
                LinkedResourceFlag = false,
                AttachmentMimeType = mimeType
            };

            return Result.OkIf(_mailRepository.InsertWithAttachment(data, fileObject), "Save failed.");
        }

        public AttachmentDto GetAttachment(int id)
        {
            return _mapper.ToAttachmentDto(_mailRepository.GetAttachment(id));
        }
    }
}

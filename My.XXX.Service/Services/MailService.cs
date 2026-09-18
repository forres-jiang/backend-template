using FluentValidation;
using LinqToDB;
using LinqToDB.Data;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using My.XXX.Data;
using My.XXX.Data.PersistantObjects;
using My.XXX.Infra;
using My.XXX.Service.DTOs;
using My.XXX.Service.Interfaces;
using My.XXX.Service.Mapping;
using System;
using System.Collections.Generic;
using System.Linq;

namespace My.XXX.Service
{
    public class MailService : IMailService, IScopeDependency
    {
        private readonly AppCenterConfig _appCenterConfig;
        private readonly ILogger<MailService> _logger;
        private readonly IValidator<Mail> _validator;
        private readonly MailContext _mailContext;
        private readonly ApplicationMapper _mapper;

        public MailService(
            IOptionsMonitor<AppCenterConfig> appConfig,
            ILogger<MailService> logger,
            IValidator<Mail> validator,
            MailContext mailContext,
            ApplicationMapper mapper)
        {
            _appCenterConfig = appConfig.CurrentValue;
            _mailContext = mailContext;
            _validator = validator;
            _mapper = mapper;
            _logger = logger;
        }

        public PwCResult SendEmail(Mail mail)
        {
            var validate = _validator.Validate(mail);
            if (!validate.IsValid)
            {
                return PwCResult.Fail(validate.Errors);
            }
            var date = DateTime.Now;
            var model = _mapper.ToMailQueue(mail);
            model.APPCODE = _appCenterConfig.AppCode;
            model.REPLYTO = "DO NOT REPLY";
            model.ORGANISATION = "PwC";
            model.POSTEDFLAG = 'N';
            model.SUBMITDATE = date;
            model.IMMEDIATEFLAG = 'Y';
            model.ENCODE = "utf-8";
            model.SUBMITBY = "System";
            model.SENDDATE = date;
            var value = _mailContext.Insert(model) > 0 ? PwCResult.Success() : PwCResult.Fail("Failed to send mail.");
            return value;
        }

        public List<MailQueue> GetEmail()
        {
            return _mailContext.MailQueues.Take(10).ToList();
        }

        public PwCResult BatchInsertEmail()
        {
            var list = new List<MailQueue>();
            for (int i = 0; i < 10; i++)
            {
                var model = new MailQueue
                {
                    MFROM = "CNHK GTS SDC Support",
                    MTO = "Forres Jiang/CN/GTS/PwC",
                    SUBMITBY = "Test" + DateTime.Now.ToString("yyMMddHHmmssfff"),
                    CONTENT = "Test" + DateTime.Now.ToString("yyMMddHHmmssfff"),
                    SENDDATE = DateTime.Now,
                    APPCODE = _appCenterConfig.AppCode,
                    REPLYTO = "DO NOT REPLY",
                    ORGANISATION = "PwC",
                    POSTEDFLAG = ' ',
                    SUBMITDATE = DateTime.Now,
                    IMMEDIATEFLAG = 'Y',
                    ENCODE = "utf-8"
                };
                list.Add(model);
            }

            var result = _mailContext.BulkCopy(list);
            return PwCResult.Success(result);
        }

        public bool SendEmailWithFile(Mail mail, byte[] fileData, string fileName, string mimeType)
        {
            var currnetDate = DateTime.Now;
            var data = new MailQueue()
            {
                APPCODE = _appCenterConfig.AppCode,
                MTO = mail.MTO,
                CC = mail.CC,
                BCC = string.Empty,
                ORGANISATION = "PwC",
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
                return _mailContext.Insert(data) > 0;
            }

            var fileObject = new Attachment
            {
                AttachmentContent = fileData,
                AttachmentFileName = fileName,
                AttachmentName = fileName,
                LinkedResourceFlag = false,
                AttachmentMimeType = mimeType
            };

            try
            {
                _mailContext.BeginTransaction();
                var AttachmentId = _mailContext.InsertWithInt32Identity(fileObject);
                var MailSeq = _mailContext.InsertWithInt32Identity(data);
                var mapData = new AttachmentMapping
                {
                    MailSeq = MailSeq,
                    AttachmentId = AttachmentId
                };
                var result = _mailContext.Insert(mapData);
                _mailContext.CommitTransaction();
                return result > 0;
            }
            catch (Exception ex)
            {
                _mailContext.RollbackTransaction();
                _logger.LogError(ex, "send email error.");
                return false;
            }
        }

        public Attachment GetAttachment(int id)
        {
            return _mailContext.Attachments.Where(m => m.AttachmentId == id).FirstOrDefault();
        }
    }
}
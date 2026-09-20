using FluentResults;
using FluentValidation;
using My.XXX.Service.DTOs;
using My.XXX.Service.Interfaces;
using My.XXX.Service.Ports;
using My.XXX.Shared;
using System;
using System.Collections.Generic;
using System.Linq;
namespace My.XXX.Service;
public sealed class MailService(IValidator<Mail> validator, IMailRepository repository) : IMailService, IScopeDependency
{
    private Result Validate(Mail mail)
    {
        if (mail == null) return Result.Fail("Mail is required.");
        var validation = validator.Validate(mail);
        return validation.IsValid ? Result.Ok() : Result.Fail(validation.Errors.Select(error =>
            new Error(error.ErrorMessage).WithMetadata("PropertyName", error.PropertyName).WithMetadata("ErrorCode", error.ErrorCode)));
    }
    public Result SendEmail(Mail mail) => SendEmailWithFile(mail, null, null, null);
    public Result SendEmailWithFile(Mail mail, byte[] fileData, string fileName, string mimeType)
    {
        var validation = Validate(mail);
        if (validation.IsFailed) return validation;
        var attachment = fileData == null || fileData.Length == 0 ? null : new AttachmentDto
        {
            AttachmentContent = fileData, AttachmentFileName = fileName,
            AttachmentName = fileName, AttachmentMimeType = mimeType
        };
        return Result.OkIf(repository.Enqueue(mail, attachment), "Failed to send mail.");
    }
    public List<MailQueueDto> GetEmail() => repository.GetRecent(10);
    public AttachmentDto GetAttachment(int id) => repository.GetAttachment(id);
    // Retained sample operation for source compatibility; not an HTTP endpoint.
    public Result<BatchWriteSummary> BatchInsertEmail()
    {
        var mails = Enumerable.Range(0, 10).Select(i => new Mail
        {
            MFROM = "CNHK GTS SDC Support", MTO = "Forres Jiang/CN/GTS/xxx",
            CONTENT = "Test" + DateTime.Now.ToString("yyMMddHHmmssfff"), SENDDATE = DateTime.Now
        }).ToList();
        var result = repository.EnqueueBatch(mails);
        return result.RowsCopied == mails.Count ? Result.Ok(result) : Result.Fail<BatchWriteSummary>("Failed to send mail.");
    }
}

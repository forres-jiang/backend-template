using FluentValidation;
using My.XXX.Service.DTOs;
using System;

namespace My.XXX.Service.Validators
{
    public class MailValidator : AbstractValidator<Mail>
    {
        public MailValidator()
        {
            RuleFor(m => m.MTO).NotEmpty();
            RuleFor(m => m.MFROM).NotEmpty();
            RuleFor(m => m.SUBJECT).NotEmpty();
            RuleFor(m => m.CONTENT).NotEmpty();
            RuleFor(m => m.SENDDATE).Must(IsValidDate);
        }

        private bool IsValidDate(DateTime date)
        {
            return !date.Equals(default);
        }
    }
}
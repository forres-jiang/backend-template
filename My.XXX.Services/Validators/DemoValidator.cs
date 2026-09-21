using FluentValidation;
using My.XXX.Service.DTOs;

namespace My.XXX.Services.Validators
{
    public class DemoValidator : AbstractValidator<DemoModel>
    {
        public DemoValidator()
        {
            //https://docs.fluentvalidation.net/en/latest/custom-validators.html
            RuleFor(m => m.DemoString).NotEmpty().MinimumLength(0);
            RuleFor(m => m.DemoInt).NotEqual(0);
        }
    }
}
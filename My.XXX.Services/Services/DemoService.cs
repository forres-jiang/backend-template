using FluentResults;
using FluentValidation;
using My.XXX.Contracts.DTOs;
using My.XXX.Services.Interfaces;
using My.XXX.Services.Ports;
using My.XXX.Shared;
using System.Linq;
namespace My.XXX.Services;

public sealed class DemoService(IValidator<DemoModel> validator, IDemoRepository repository) : IDemoService
{
    public Result Save(DemoModel model)
    {
        var validation = validator.Validate(model);
        return validation.IsValid ? Result.OkIf(repository.Add(model), "Save failed.")
            : Result.Fail(validation.Errors.Select(e => e.ErrorMessage));
    }
    public Result Update(DemoModel model)
    {
        var validation = validator.Validate(model);
        return validation.IsValid ? Result.OkIf(repository.Update(model) > 0, "Save failed.")
            : Result.Fail(validation.Errors.Select(e => e.ErrorMessage));
    }
    public void ExecProc() => repository.QueryProcMultiple();
}

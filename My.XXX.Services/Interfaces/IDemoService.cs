using FluentResults;
using My.XXX.Contracts.DTOs;

namespace My.XXX.Services.Interfaces
{
    public interface IDemoService
    {
        Result Save(DemoModel model);

        Result Update(DemoModel model);

        public void ExecProc();
    }
}
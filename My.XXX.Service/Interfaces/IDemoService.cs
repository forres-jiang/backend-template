using FluentResults;
using My.XXX.Infra;
using My.XXX.Service.DTOs;

namespace My.XXX.Service.Interfaces
{
    public interface IDemoService
    {
        Result Save(DemoModel model);

        Result Update(DemoModel model);

        public void ExecProc();
    }
}